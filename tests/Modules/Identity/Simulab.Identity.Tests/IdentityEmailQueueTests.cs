using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Jobs;
using Simulab.Jobs.Email;
using Simulab.Jobs.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-13 AC1-AC4, AC7 and AC11: the identity emails leave through the job queue, not the request.</summary>
public sealed class IdentityEmailQueueTests : IdentityApiTests
{
    private const string RegistrationsRoute = "/api/v1/identity/registrations";
    private const string ResetRequestsRoute = "/api/v1/identity/password-reset-requests";

    /// <summary>Set by the one test that needs the token write to fail inside its own transaction (AC4).</summary>
    private bool _breakTheTokenWrite;

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            if (!_breakTheTokenWrite)
            {
                return;
            }

            services.RemoveAll<IEmailVerificationTokenStore>();
            services.AddScoped<IEmailVerificationTokenStore>(provider =>
                new TooLongTokenStore(new EmailVerificationTokenStore(provider.GetRequiredService<IdentityModuleDbContext>())));
        });

    // AC2, AC11.
    [Fact]
    public async Task SignUp_LeavesTheVerificationEmailAsOnePendingJobCarryingTheRenderedMessage()
    {
        var client = Client("pt-BR");
        var form = SignUpForm.Valid("fila@exemplo.com");

        await PostAsync(client, RegistrationsRoute, form, HttpStatusCode.Accepted);

        Emails.Count.Should().Be(0, "the request answers before anything reaches the mail server");
        var jobs = await JobsAsync();
        jobs.Should().ContainSingle();
        jobs[0].Type.Should().Be(EmailJob.Type);
        jobs[0].Status.Should().Be(JobStatus.Pending);
        jobs[0].Attempts.Should().Be(0);
        jobs[0].RunAfter.Should().Be(Factory.Clock.GetUtcNow());

        // BR4: the message was written inside the request, in the recipient's language.
        var message = EmailJob.Deserialize(jobs[0].Payload);
        message.To.Should().Be(form.Email);
        message.Subject.Should().StartWith("Confirme seu e-mail");
        message.HtmlBody.Should().Contain("https://localhost/verify-email?token=");

        // The token it points at is stored in the same transaction, hashed (BR2).
        (await QueryAsync(context => context.EmailVerificationTokens.CountAsync())).Should().Be(1);
    }

    // AC3, AC7.
    [Fact]
    public async Task RunningTheQueue_SendsExactlyTheStoredMessageAndLeavesNoRow()
    {
        var client = Client("pt-BR");
        await PostAsync(client, RegistrationsRoute, SignUpForm.Valid("entrega@exemplo.com"), HttpStatusCode.Accepted);
        var stored = EmailJob.Deserialize((await JobsAsync())[0].Payload);

        (await Factory.RunJobsAsync()).Should().Be(1);

        Emails.Count.Should().Be(1);
        Emails.Last.Should().Be(stored);
        (await JobsAsync()).Should().BeEmpty("BR6: a job that succeeds leaves no row");
    }

    // AC1.
    [Fact]
    public async Task ResetRequest_KnownAndUnknownAddress_NeitherTouchesTheMailServer()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await RunJobsAsync();
        Emails.Clear();

        await PostAsync(client, ResetRequestsRoute, new RequestPasswordResetRequest("ninguem@exemplo.com"), HttpStatusCode.Accepted);

        Emails.Count.Should().Be(0);
        (await JobsAsync()).Should().BeEmpty("an unknown address enqueues nothing either");

        await PostAsync(client, ResetRequestsRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);

        // Same status, same body, and neither path waited for SMTP: only the queue tells them apart.
        Emails.Count.Should().Be(0);
        (await PendingJobCountAsync()).Should().Be(1);
    }

    // AC4.
    [Fact]
    public async Task SignUp_WhenTheTokenWriteFails_LeavesNoJobBehind()
    {
        _breakTheTokenWrite = true;
        await using var factory = new IdentityApiFactory { ConfigureHost = ConfigureHost };
        await factory.PrepareAsync($"{nameof(IdentityEmailQueueTests)}_rollback");
        var client = factory.CreateClient("en");

        using var response = await client.PostAsJsonAsync(
            RegistrationsRoute, SignUpForm.Valid("rollback@exemplo.com"), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await using var scope = factory.Services.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        (await jobs.Jobs.CountAsync()).Should().Be(0, "BR2: the job shares the transaction that failed");
        factory.Emails.Count.Should().Be(0);
    }

    // AC11: the worker exists in the host and is switched off here, which is why nothing above waits.
    [Fact]
    public void TestHost_RegistersTheWorkerAndKeepsItOff()
    {
        Factory.Services.GetServices<IHostedService>().Should().ContainItemsAssignableTo<JobWorker>();
        Factory.Services.GetRequiredService<IOptions<JobOptions>>().Value.WorkerEnabled.Should().BeFalse();
    }

    /// <summary>
    /// Writes a hash longer than its column, so the failure happens inside the store's own
    /// <c>SaveChanges</c> — the very transaction the job was staged on.
    /// </summary>
    private sealed class TooLongTokenStore(IEmailVerificationTokenStore inner) : IEmailVerificationTokenStore
    {
        public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(token);
            return inner.AddAsync(
                new EmailVerificationToken
                {
                    UserId = token.UserId,
                    TokenHash = new string('x', 100),
                    ExpiresAt = token.ExpiresAt
                },
                cancellationToken);
        }

        public Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            inner.FindByHashAsync(tokenHash, cancellationToken);

        public Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.ConsumePendingForUserAsync(userId, consumedAt, cancellationToken);

        public Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            inner.CountCreatedSinceAsync(userId, since, cancellationToken);

        public Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.LastCreatedAtAsync(userId, cancellationToken);

        public Task ConsumeAsync(EmailVerificationToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.ConsumeAsync(token, consumedAt, cancellationToken);
    }
}
