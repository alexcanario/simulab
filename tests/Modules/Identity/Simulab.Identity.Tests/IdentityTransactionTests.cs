using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Email;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Identity.Infrastructure.Sessions;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-47: the password reset and change, the reset and verification link requests and the two-factor confirmation
/// commit every write of their success path in one transaction, or none of it (AC1-AC8, AC10, AC11). A failure
/// is injected by a double of a store, a mailer or the user store the handler already takes, never by breaking
/// the database. AC9 is the existing suites staying green; AC12 is the missing-key test, since no text changed.
/// </summary>
public sealed class IdentityTransactionTests : IdentityApiTests
{
    private const string RequestRoute = "/api/v1/identity/password-reset-requests";
    private const string ResetRoute = "/api/v1/identity/password-resets";
    private const string ChangeRoute = "/api/v1/identity/password-changes";
    private const string VerifyRoute = "/api/v1/identity/email-verifications";
    private const string ResendRoute = "/api/v1/identity/email-verifications/resend";
    private const string NewPassword = "Revisar#2026!x";

    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    private readonly Faults _faults = new();

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPasswordMailer>();
            services.AddScoped<IPasswordMailer>(provider =>
                new FailingPasswordMailer(ActivatorUtilities.CreateInstance<PasswordMailer>(provider), _faults));

            services.RemoveAll<IRefreshSessionStore>();
            services.AddScoped<IRefreshSessionStore>(provider =>
                new FailingAfterRevokeSessionStore(ActivatorUtilities.CreateInstance<RedisRefreshSessionStore>(provider), _faults));

            services.RemoveAll<IPasswordResetTokenStore>();
            services.AddScoped<IPasswordResetTokenStore>(provider =>
                new FailingPasswordResetTokenStore(new PasswordResetTokenStore(provider.GetRequiredService<IdentityModuleDbContext>()), _faults));

            services.RemoveAll<IEmailVerificationTokenStore>();
            services.AddScoped<IEmailVerificationTokenStore>(provider =>
                new FailingVerificationTokenStore(new EmailVerificationTokenStore(provider.GetRequiredService<IdentityModuleDbContext>()), _faults));

            // A subclass, so every store interface UserManager probes stays implemented.
            services.RemoveAll<IUserStore<User>>();
            services.AddScoped<IUserStore<User>>(provider =>
                new FailingUserStore(provider.GetRequiredService<IdentityModuleDbContext>(), _faults));
        });
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    // AC1, AC10, AC11.
    [Fact]
    public async Task Reset_FailsAfterThePasswordIsSetAndBeforeTheCommit_ChangesNothingAndTheLinkStillWorks()
    {
        var client = Client();
        var email = $"redefinir.{Guid.CreateVersion7():N}@exemplo.com";
        await PostAsync(client, "/api/v1/identity/registrations", SignUpForm.Valid(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        var token = await RequestLinkAsync(client, email);
        await UpdateUserAsync(email, user =>
        {
            user.AccessFailedCount = 3;
            user.LockoutEnd = Now.AddMinutes(15);
        });
        var before = await UserAsync(email);
        var jobsBefore = (await JobsAsync()).Count;
        var eventsBefore = await EventCountAsync(before.Id, AccountEventType.PasswordResetCompleted);

        _faults.FailPasswordNotice = true;
        using var response = await client.PostAsJsonAsync(ResetRoute, new ResetPasswordRequest(token, NewPassword), AppJson.Options);

        ShouldBeTheHostsProblem500(response);
        var after = await UserAsync(email);
        after.PasswordHash.Should().Be(before.PasswordHash, "AC1: the password is the old one");
        after.AccessFailedCount.Should().Be(3);
        after.LockoutEnd.Should().Be(before.LockoutEnd);
        after.Status.Should().Be(AccountStatus.Pending);
        (await QueryAsync(context => context.PasswordResetTokens.CountAsync(candidate => candidate.UserId == before.Id && candidate.ConsumedAt != null)))
            .Should().Be(0, "AC1: the link is not consumed");
        (await QueryAsync(context => context.EmailVerificationTokens.CountAsync(candidate => candidate.UserId == before.Id && candidate.ConsumedAt == null)))
            .Should().Be(1, "AC1: the pending account's verification link is not consumed either");
        (await JobsAsync()).Should().HaveCount(jobsBefore, "AC1: no notice job");
        (await EventCountAsync(before.Id, AccountEventType.PasswordResetCompleted)).Should().Be(eventsBefore, "AC10: no event of a request that rolled back");

        _faults.FailPasswordNotice = false;
        await PostAsync(client, ResetRoute, new ResetPasswordRequest(token, NewPassword), HttpStatusCode.NoContent);
        (await TokenClient.SignInAsync(client, email, NewPassword)).AccessToken.Should().NotBeNull("AC1: the same link then resets the password");
        (await EventCountAsync(before.Id, AccountEventType.PasswordResetCompleted)).Should().Be(eventsBefore + 1, "AC10: after the commit, the event");
    }

    // AC2, AC10, AC11.
    [Fact]
    public async Task Change_FailsAfterThePasswordIsChangedAndBeforeTheSessions_KeepsTheOldPasswordAndTheOtherSession()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 2);
        await UpdateUserAsync(email, user => user.AccessFailedCount = 2);
        var before = await UserAsync(email);
        await RunJobsAsync();
        var jobsBefore = (await JobsAsync()).Count;

        _faults.FailPasswordNotice = true;
        using var response = await ChangeAsync(client, sessions[0]);

        ShouldBeTheHostsProblem500(response);
        var after = await UserAsync(email);
        after.PasswordHash.Should().Be(before.PasswordHash);
        after.AccessFailedCount.Should().Be(2, "AC2: the count is unchanged");
        (await JobsAsync()).Should().HaveCount(jobsBefore, "AC2: no password changed job");
        (await EventCountAsync(before.Id, AccountEventType.PasswordChanged)).Should().Be(0, "AC10");
        (await TokenClient.RefreshAsync(client, sessions[1].RefreshToken!)).AccessToken.Should().NotBeNull("AC2: the other session still refreshes");
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull("AC2: the old password still works");
    }

    // AC3.
    [Fact]
    public async Task Change_FailsAfterTheSessionsWereRevoked_RevokesTheOtherSessionAndKeepsTheOldPassword()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 2);
        var before = await UserAsync(email);

        _faults.FailAfterRevoke = true;
        using var response = await ChangeAsync(client, sessions[0]);

        ShouldBeTheHostsProblem500(response);
        (await UserAsync(email)).PasswordHash.Should().Be(before.PasswordHash, "AC3: the password is the old one");
        (await TokenClient.RefreshAsync(client, sessions[1].RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid, "AC3: the other session is revoked");

        _faults.FailAfterRevoke = false;
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
    }

    // AC3.
    [Fact]
    public async Task Reset_FailsAfterTheSessionsWereRevoked_RevokesTheSessionAndKeepsTheOldPassword()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var token = await RequestLinkAsync(client, email);
        var before = await UserAsync(email);

        _faults.FailAfterRevoke = true;
        using var response = await client.PostAsJsonAsync(ResetRoute, new ResetPasswordRequest(token, NewPassword), AppJson.Options);

        ShouldBeTheHostsProblem500(response);
        (await UserAsync(email)).PasswordHash.Should().Be(before.PasswordHash, "AC3: the password is the old one");
        (await TokenClient.RefreshAsync(client, session.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid, "AC3: the session is revoked");
    }

    // AC4, AC10.
    [Fact]
    public async Task RequestLink_FailsAfterTheOlderLinksAreConsumed_TheOlderLinkStillWorksAndNothingNewExists()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var older = await RequestLinkAsync(client, email);
        Factory.Clock.Advance(PasswordResetPolicy.Cooldown);
        var userId = (await UserAsync(email)).Id;
        await RunJobsAsync();
        var jobsBefore = (await JobsAsync()).Count;
        var eventsBefore = await EventCountAsync(userId, AccountEventType.PasswordResetRequested);

        _faults.FailPasswordResetLinkAdd = true;
        using var response = await client.PostAsJsonAsync(RequestRoute, new RequestPasswordResetRequest(email), AppJson.Options);

        ShouldBeTheHostsProblem500(response);
        var tokens = await QueryAsync(context => context.PasswordResetTokens.Where(token => token.UserId == userId).ToListAsync());
        tokens.Should().ContainSingle("AC4: no new link").Which.IsConsumed.Should().BeFalse("AC4: the older link is not consumed");
        (await JobsAsync()).Should().HaveCount(jobsBefore, "AC4: no new job");
        (await EventCountAsync(userId, AccountEventType.PasswordResetRequested)).Should().Be(eventsBefore, "AC10");

        _faults.FailPasswordResetLinkAdd = false;
        await PostAsync(client, ResetRoute, new ResetPasswordRequest(older, NewPassword), HttpStatusCode.NoContent);
    }

    // AC5.
    [Fact]
    public async Task Resend_FailsAfterTheOlderLinksAreConsumed_TheOlderLinkStillVerifiesAndNothingNewExists()
    {
        var client = Client();
        var email = $"reenviar.{Guid.CreateVersion7():N}@exemplo.com";
        await PostAsync(client, "/api/v1/identity/registrations", SignUpForm.Valid(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        var older = VerificationLink.TokenOf(Emails.Last!.HtmlBody);
        Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown);
        var userId = (await UserAsync(email)).Id;
        var jobsBefore = (await JobsAsync()).Count;

        _faults.FailVerificationLinkAdd = true;
        using var response = await client.PostAsJsonAsync(ResendRoute, new ResendVerificationRequest(email), AppJson.Options);

        ShouldBeTheHostsProblem500(response);
        var tokens = await QueryAsync(context => context.EmailVerificationTokens.Where(token => token.UserId == userId).ToListAsync());
        tokens.Should().ContainSingle("AC5: no new link").Which.IsConsumed.Should().BeFalse("AC5: the older link is not consumed");
        (await JobsAsync()).Should().HaveCount(jobsBefore, "AC5: no new job");

        _faults.FailVerificationLinkAdd = false;
        await PostAsync(client, VerifyRoute, new VerifyEmailRequest(older), HttpStatusCode.OK);
    }

    // AC6, AC10.
    [Fact]
    public async Task ConfirmTotp_FailsWhileStoringTheRecoveryCodes_LeavesTwoFactorOffAndNoCodes()
    {
        var (client, email, session, secret) = await StartedEnrolmentAsync();
        var userId = (await UserAsync(email)).Id;

        _faults.FailRecoveryCodeStore = true;
        using var response = await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(secret, Now));

        ShouldBeTheHostsProblem500(response);
        (await UserAsync(email)).TwoFactorEnabled.Should().BeFalse("AC6: two-factor is off");
        (await RecoveryRowAsync(userId)).Should().BeNull("AC6: no recovery codes stored");
        (await EventCountAsync(userId, AccountEventType.TwoFactorEnabled)).Should().Be(0, "AC10");
    }

    // AC7.
    [Fact]
    public async Task ConfirmTotp_FailsAfterARightCode_TheSpentStepStaysSpentAndTheNextCodeConfirms()
    {
        var (client, email, session, secret) = await StartedEnrolmentAsync();
        var code = TotpApi.CodeAt(secret, Now);

        _faults.FailRecoveryCodeStore = true;
        using var failed = await TotpApi.ConfirmAsync(client, session.AccessToken!, code);
        ShouldBeTheHostsProblem500(failed);

        _faults.FailRecoveryCodeStore = false;
        using var replay = await TotpApi.ConfirmAsync(client, session.AccessToken!, code);
        replay.IsSuccessStatusCode.Should().BeFalse("AC7: the same code is refused, its step stays spent");
        CodeOf(await replay.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.TotpCodeInvalid);

        Factory.Clock.Advance(Step);
        var confirmed = await TotpApi.ReadAsync<RecoveryCodesResponse>(
            await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(secret, Now)));
        confirmed.Codes.Should().HaveCount(RecoveryCodes.Count, "AC7: the next code confirms");
        (await UserAsync(email)).TwoFactorEnabled.Should().BeTrue();
    }

    // AC8.
    [Fact]
    public async Task ConfirmTotp_StoreReportsAFailedResultForTheRecoveryCodes_DoesNotAnswerSuccessAndLeavesTwoFactorOff()
    {
        var (client, email, session, secret) = await StartedEnrolmentAsync();

        _faults.FailRecoveryCodeResult = true;
        using var response = await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(secret, Now));

        response.IsSuccessStatusCode.Should().BeFalse("AC8: never a success with codes that were not stored");
        ShouldBeTheHostsProblem500(response);
        (await UserAsync(email)).TwoFactorEnabled.Should().BeFalse("AC8");
    }

    private async Task<(HttpClient Client, string Email, TokenResponse Session, string Secret)> StartedEnrolmentAsync()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        return (client, email, session, enrolment.Secret);
    }

    private async Task<string> RequestLinkAsync(HttpClient client, string email)
    {
        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(client, RequestRoute, new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        return ResetLink.TokenOf(Emails.Last!.HtmlBody);
    }

    private static async Task<HttpResponseMessage> ChangeAsync(HttpClient client, TokenResponse session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ChangeRoute)
        {
            Content = JsonContent.Create(new ChangePasswordRequest(SignUpForm.ValidPassword, NewPassword), options: AppJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.SendAsync(request);
    }

    /// <summary>AC11: the host's existing answer to an unhandled error, with no new code or text.</summary>
    private static void ShouldBeTheHostsProblem500(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private Task<User> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

    private Task<int> UpdateUserAsync(string email, Action<User> change) =>
        QueryAsync(async context =>
        {
            var user = await context.Users.SingleAsync(candidate => candidate.Email == email);
            change(user);
            return await context.SaveChangesAsync();
        });

    private Task<int> EventCountAsync(Guid userId, AccountEventType type) =>
        QueryAsync(context => context.AccountEvents.CountAsync(accountEvent => accountEvent.UserId == userId && accountEvent.Type == type));

    private Task<string?> RecoveryRowAsync(Guid userId) =>
        QueryAsync(context => context.UserTokens
            .Where(token => token.UserId == userId && token.LoginProvider == RecoveryCodes.TokenProvider && token.Name == RecoveryCodes.TokenName)
            .Select(token => token.Value)
            .SingleOrDefaultAsync());

    /// <summary>The faults a test arms; each double reads its own flag when the handler reaches it.</summary>
    private sealed class Faults
    {
        public bool FailPasswordNotice { get; set; }

        public bool FailAfterRevoke { get; set; }

        public bool FailPasswordResetLinkAdd { get; set; }

        public bool FailVerificationLinkAdd { get; set; }

        public bool FailRecoveryCodeStore { get; set; }

        public bool FailRecoveryCodeResult { get; set; }
    }

    private sealed class FailingPasswordMailer(IPasswordMailer inner, Faults faults) : IPasswordMailer
    {
        public Task SendResetLinkAsync(string email, string rawToken, string locale, CancellationToken cancellationToken = default) =>
            inner.SendResetLinkAsync(email, rawToken, locale, cancellationToken);

        public Task SendPasswordChangedAsync(string email, DateTimeOffset changedAt, string locale, CancellationToken cancellationToken = default) =>
            faults.FailPasswordNotice
                ? throw new InvalidOperationException("Injected: the notice could not be staged.")
                : inner.SendPasswordChangedAsync(email, changedAt, locale, cancellationToken);
    }

    /// <summary>Calls the real store, then throws: what a crash right after the sessions ended looks like (AC3).</summary>
    private sealed class FailingAfterRevokeSessionStore(IRefreshSessionStore inner, Faults faults) : IRefreshSessionStore
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
            await inner.RevokeAllAsync(userId, exceptSessionJti, cancellationToken);
            if (faults.FailAfterRevoke)
            {
                throw new InvalidOperationException("Injected: the commit failed after the sessions ended.");
            }
        }

        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.CountActiveAsync(userId, cancellationToken);

        public Task RestampAsync(string sessionJti, string? securityStamp, CancellationToken cancellationToken = default) =>
            inner.RestampAsync(sessionJti, securityStamp, cancellationToken);
    }

    private sealed class FailingPasswordResetTokenStore(IPasswordResetTokenStore inner, Faults faults) : IPasswordResetTokenStore
    {
        public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
            faults.FailPasswordResetLinkAdd
                ? throw new InvalidOperationException("Injected: the new link could not be saved.")
                : inner.AddAsync(token, cancellationToken);

        public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            inner.FindByHashAsync(tokenHash, cancellationToken);

        public Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.ConsumePendingForUserAsync(userId, consumedAt, cancellationToken);

        public Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            inner.CountCreatedSinceAsync(userId, since, cancellationToken);

        public Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default) =>
            inner.LastCreatedAtAsync(userId, cancellationToken);

        public Task<bool> TryConsumeAsync(PasswordResetToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default) =>
            inner.TryConsumeAsync(token, consumedAt, cancellationToken);
    }

    private sealed class FailingVerificationTokenStore(IEmailVerificationTokenStore inner, Faults faults) : IEmailVerificationTokenStore
    {
        public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default) =>
            faults.FailVerificationLinkAdd
                ? throw new InvalidOperationException("Injected: the new link could not be saved.")
                : inner.AddAsync(token, cancellationToken);

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

    /// <summary>
    /// Only <see cref="SetTokenAsync"/> is changed, and only for the recovery codes' own row: it throws (AC6, AC7),
    /// or stores the row and makes the update that follows it report a failed result (AC8).
    /// </summary>
    private sealed class FailingUserStore(IdentityModuleDbContext context, Faults faults)
        : UserStore<User, Role, IdentityModuleDbContext, Guid>(context)
    {
        private bool _failTheNextUpdate;

        public override async Task SetTokenAsync(User user, string loginProvider, string name, string? value, CancellationToken cancellationToken = default)
        {
            var isRecoveryRow = loginProvider == RecoveryCodes.TokenProvider && name == RecoveryCodes.TokenName;
            if (isRecoveryRow && faults.FailRecoveryCodeStore)
            {
                throw new InvalidOperationException("Injected: the recovery codes could not be stored.");
            }

            await base.SetTokenAsync(user, loginProvider, name, value, cancellationToken);
            _failTheNextUpdate = isRecoveryRow && faults.FailRecoveryCodeResult;
        }

        public override Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (!_failTheNextUpdate)
            {
                return base.UpdateAsync(user, cancellationToken);
            }

            _failTheNextUpdate = false;
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "Injected", Description = "Injected failure." }));
        }
    }
}
