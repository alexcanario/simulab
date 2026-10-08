using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Account;
using Simulab.Identity.Infrastructure.Sessions;
using Simulab.Jobs;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-59: what an erasure still owes after it commits survives a crash. The crash is a double of the session
/// store the handler and the job already take: it throws on the calls the test names, before reaching Redis.
/// </summary>
public sealed class AccountErasureFollowUpTests : IdentityApiTests
{
    private const string EraseRoute = "/api/v1/identity/account-erasures";

    private readonly Journal _journal = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(_journal);
            services.RemoveAll<IRefreshSessionStore>();
            services.AddScoped<IRefreshSessionStore>(provider =>
                new CrashingSessionStore(ActivatorUtilities.CreateInstance<RedisRefreshSessionStore>(provider), _journal));
            services.AddIntegrationEventConsumer<UserErased, JournalConsumer>();
        });

    private static async Task<HttpResponseMessage> EraseAsync(HttpClient client, TokenResponse session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EraseRoute)
        {
            Content = JsonContent.Create(new EraseAccountRequest(SignUpForm.ValidPassword), options: AppJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.SendAsync(request);
    }

    private async Task<(Guid UserId, string Email, IReadOnlyList<TokenResponse> Sessions)> SignedInAsync(HttpClient client, int devices)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, devices);
        var userId = await QueryAsync(context => Task.FromResult(context.Users.Single(user => user.Email == email).Id));
        return (userId, email, sessions);
    }

    private static async Task AssertRefusedAsync(HttpClient client, IEnumerable<TokenResponse> sessions)
    {
        foreach (var session in sessions)
        {
            (await SignedInSessions.AnswerOfAsync(client, session.AccessToken!))
                .Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));
        }
    }

    // AC1.
    [Fact]
    public async Task Erase_StagesOneAccountErasedJobWithTheUserIdAndTheTime()
    {
        var client = Client();
        var (userId, _, sessions) = await SignedInAsync(client, 1);

        using var response = await EraseAsync(client, sessions[0]);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var job = (await JobsAsync()).Should().ContainSingle(candidate => candidate.Type == AccountErasedJob.Type).Subject;
        job.Status.Should().Be(JobStatus.Pending);
        using var payload = JsonDocument.Parse(job.Payload);
        payload.RootElement.GetProperty("userId").GetGuid().Should().Be(userId);
        payload.RootElement.GetProperty("erasedAt").GetDateTimeOffset().Should().Be(Factory.Clock.GetUtcNow());
    }

    // AC3.
    [Fact]
    public async Task Erase_RefusesEveryTokenBeforeTheAnswer_AndPublishesOnlyWhenTheJobRuns()
    {
        var client = Client();
        var (userId, _, sessions) = await SignedInAsync(client, 2);

        using var response = await EraseAsync(client, sessions[0]);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssertRefusedAsync(client, sessions);
        _journal.Published.Should().BeEmpty();

        await RunJobsAsync();

        _journal.Published.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }

    // AC4: the process dies right after the commit, before the request's own revocation.
    [Fact]
    public async Task Erase_WhenTheProcessDiesAfterTheCommit_TheJobRevokesThenPublishes()
    {
        var client = Client();
        var (userId, _, sessions) = await SignedInAsync(client, 2);
        _journal.CrashOnRevokeCall(1);

        using var response = await EraseAsync(client, sessions[0]);

        response.IsSuccessStatusCode.Should().BeFalse();
        _journal.Published.Should().BeEmpty();

        await RunJobsAsync();

        await AssertRefusedAsync(client, sessions);
        _journal.Published.Should().ContainSingle().Which.UserId.Should().Be(userId);
        // F-10 BR13: the tokens stopped before a consumer heard.
        _journal.Steps.Should().ContainInOrder("revoked", "published");
    }

    // AC5: at least once, never a failure the second time.
    [Fact]
    public async Task Job_RunTwice_SucceedsAndPublishesAgain()
    {
        var client = Client();
        var (userId, _, sessions) = await SignedInAsync(client, 1);
        using (await EraseAsync(client, sessions[0]))
        {
        }

        var payload = (await JobsAsync()).Single(job => job.Type == AccountErasedJob.Type).Payload;
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetServices<IJobHandler>().Single(candidate => candidate.Type == AccountErasedJob.Type);

        await handler.HandleAsync(payload);
        await handler.HandleAsync(payload);

        _journal.Published.Should().HaveCount(2).And.OnlyContain(published => published.UserId == userId);
        await AssertRefusedAsync(client, sessions);
    }

    // AC6: the first attempt fails, the policy backs off, the next one finishes.
    [Fact]
    public async Task Job_WhenTheSessionStoreFailsOnTheFirstAttempt_IsRetriedAfterTheBackoff()
    {
        var client = Client();
        var (userId, _, sessions) = await SignedInAsync(client, 1);
        _journal.CrashOnRevokeCall(2); // call 1 is the request's own revocation, call 2 the job's first attempt

        using var response = await EraseAsync(client, sessions[0]);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await RunJobsAsync();
        _journal.Published.Should().BeEmpty();
        (await PendingJobCountAsync()).Should().BeGreaterThan(0);
        (await JobsAsync()).Single(job => job.Type == AccountErasedJob.Type).Attempts.Should().Be(1);

        Factory.Clock.Advance(JobPolicy.BackoffAfter(1));
        await RunJobsAsync();

        _journal.Published.Should().ContainSingle().Which.UserId.Should().Be(userId);
        (await JobsAsync()).Should().NotContain(job => job.Type == AccountErasedJob.Type);
    }

    /// <summary>What the doubles saw, shared by the host and the test.</summary>
    public sealed class Journal
    {
        private readonly ConcurrentQueue<string> _steps = new();
        private readonly ConcurrentQueue<UserErased> _published = new();
        private int _revokeCalls;

        private readonly ConcurrentBag<int> _crashOn = [];

        /// <summary>Crashes the nth revocation from now on (1 = the next one), so the sign-ins before it do not count.</summary>
        public void CrashOnRevokeCall(int nth) => _crashOn.Add(Volatile.Read(ref _revokeCalls) + nth);

        public bool ShouldCrash(int call) => _crashOn.Contains(call);

        public IReadOnlyList<string> Steps => [.. _steps];

        public IReadOnlyList<UserErased> Published => [.. _published];

        public int NextRevokeCall() => Interlocked.Increment(ref _revokeCalls);

        public void Record(string step) => _steps.Enqueue(step);

        public void Add(UserErased integrationEvent)
        {
            _published.Enqueue(integrationEvent);
            Record("published");
        }
    }

    private sealed class JournalConsumer(Journal journal) : IIntegrationEventConsumer<UserErased>
    {
        public Task HandleAsync(UserErased integrationEvent, CancellationToken cancellationToken = default)
        {
            journal.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    /// <summary>Throws on the revocation calls the test names, before anything reaches Redis.</summary>
    private sealed class CrashingSessionStore(IRefreshSessionStore inner, Journal journal) : IRefreshSessionStore
    {
        public Task CreateAsync(string sessionJti, Guid userId, string? securityStamp, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) =>
            inner.CreateAsync(sessionJti, userId, securityStamp, expiresAt, cancellationToken);

        public Task<RefreshSession?> ConsumeAsync(string sessionJti, CancellationToken cancellationToken = default) =>
            inner.ConsumeAsync(sessionJti, cancellationToken);

        public Task RemoveAsync(string sessionJti, CancellationToken cancellationToken = default) =>
            inner.RemoveAsync(sessionJti, cancellationToken);

        public Task RevokeAccessTokenAsync(string sessionJti, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
            inner.RevokeAccessTokenAsync(sessionJti, timeToLive, cancellationToken);

        public Task<bool> IsAccessTokenRevokedAsync(string sessionJti, CancellationToken cancellationToken = default) =>
            inner.IsAccessTokenRevokedAsync(sessionJti, cancellationToken);

        public async Task RevokeAllAsync(Guid userId, string? exceptSessionJti = null, CancellationToken cancellationToken = default)
        {
            if (journal.ShouldCrash(journal.NextRevokeCall()))
            {
                throw new InvalidOperationException("Injected: the process died before the sessions ended.");
            }

            await inner.RevokeAllAsync(userId, exceptSessionJti, cancellationToken);
            journal.Record("revoked");
        }

        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.CountActiveAsync(userId, cancellationToken);

        public Task RestampAsync(string sessionJti, string? securityStamp, CancellationToken cancellationToken = default) =>
            inner.RestampAsync(sessionJti, securityStamp, cancellationToken);
    }
}
