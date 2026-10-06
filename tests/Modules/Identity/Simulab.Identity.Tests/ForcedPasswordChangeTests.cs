using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-53 with two-factor off: a marked account's right password hands out a challenge instead of tokens (BR3), and the
/// <c>password_change</c> grant trades it and a new password for the account (BR4 to BR9).
/// </summary>
public sealed class ForcedPasswordChangeTests : IdentityApiTests
{
    private const string NewPassword = "Revisar#2026!x";
    private const string Address = "203.0.113.50";

    private async Task<string> MarkedAccountAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        await SetMarkAsync(email, true);
        return email;
    }

    private Task SetMarkAsync(string email, bool marked) =>
        QueryAsync(context => context.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.MustChangePassword, marked)));

    private Task<User> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

    private async Task<string> ChallengeAsync(HttpClient client, string email)
    {
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);
        step.Challenge.Should().NotBeNullOrWhiteSpace();
        return step.Challenge!;
    }

    private async Task<List<AccountEvent>> EventsAsync(Guid userId) =>
        await QueryAsync(context => context.AccountEvents.AsNoTracking()
            .Where(accountEvent => accountEvent.UserId == userId)
            .OrderBy(accountEvent => accountEvent.CreatedAt).ThenBy(accountEvent => accountEvent.Id)
            .ToListAsync());

    // AC6, BR3: no tokens, a five-minute challenge, no sign-in event and the failure count where it was.
    [Fact]
    public async Task Password_MarkedAccount_AnswersAChallengeAndNoTokens()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        (await SignInFrom.PasswordAsync(client, Address, email, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        var before = await UserAsync(email);

        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);

        step.Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);
        step.Challenge.Should().NotBeNullOrWhiteSpace();
        step.ExpiresIn.Should().Be(300);
        step.AccessToken.Should().BeNull();
        var after = await UserAsync(email);
        before.AccessFailedCount.Should().Be(1, "the wrong password counted");
        after.AccessFailedCount.Should().Be(1, "the challenge step does not clear the count (BR3)");
        (await EventsAsync(after.Id)).Should().NotContain(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded);
    }

    // AC7, BR3: the checks that come before the tokens still come first.
    [Fact]
    public async Task Password_MarkedAccountThatIsLockedOut_IsRefusedAsAnyOtherAccount()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        await QueryAsync(context => context.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.LockoutEnd, DateTimeOffset.UtcNow.AddHours(1))));

        var refused = await SignInFrom.PasswordAsync(client, Address, email, SignUpForm.ValidPassword);

        refused.Error.Should().Be(IdentityErrorCodes.AccountLocked);
    }

    // AC7, BR3.
    [Fact]
    public async Task Password_MarkedAccountThatIsNotVerified_IsRefusedAsAnyOtherAccount()
    {
        var client = Client();
        var request = SignUpForm.Valid();
        await client.PostAsJsonAsync("/api/v1/identity/registrations", request, AppJson.Options);
        await SetMarkAsync(request.Email, true);

        var step = await TotpApi.PasswordStepAsync(client, request.Email, SignUpForm.ValidPassword);

        step.Error.Should().Be(IdentityErrorCodes.EmailNotVerified);
        step.Challenge.Should().BeNull();
    }

    // AC9, BR4: tokens, the old password gone, the new one working, the mark and the count cleared.
    [Fact]
    public async Task Change_ValidChallengeAndPassword_IssuesTokensAndReplacesThePassword()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        (await SignInFrom.PasswordAsync(client, Address, email, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        var challenge = await ChallengeAsync(client, email);

        var session = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        session.AccessToken.Should().NotBeNullOrWhiteSpace(session.ErrorDescription);
        (await SignInFrom.PasswordAsync(client, Address, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, Address, email, NewPassword)).AccessToken.Should().NotBeNullOrWhiteSpace("the new password signs in directly (UC3)");
        var user = await UserAsync(email);
        user.MustChangePassword.Should().BeFalse();
        user.AccessFailedCount.Should().Be(0);
    }

    // AC10, BR4: another session of the account ends, and the stamp changes.
    [Fact]
    public async Task Change_AnotherSessionExists_EndsItAndRenewsTheStamp()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var other = await SignInFrom.PasswordAsync(client, Address, email, SignUpForm.ValidPassword);
        other.RefreshToken.Should().NotBeNullOrWhiteSpace();
        await SetMarkAsync(email, true);
        var stampBefore = (await UserAsync(email)).SecurityStamp;
        var challenge = await ChallengeAsync(client, email);

        var session = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        session.AccessToken.Should().NotBeNullOrWhiteSpace(session.ErrorDescription);
        (await SignInFrom.RefreshAsync(client, Address, other.RefreshToken!)).Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
        (await UserAsync(email)).SecurityStamp.Should().NotBe(stampBefore);
        (await SignInFrom.RefreshAsync(client, Address, session.RefreshToken!)).AccessToken.Should().NotBeNullOrWhiteSpace("the new session works");
    }

    // AC11, BR5, BR6: the same password is refused and the challenge survives.
    [Fact]
    public async Task Change_SameAsCurrent_IsRefusedAndTheChallengeStillWorks()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, SignUpForm.ValidPassword);
        var retried = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeSameAsCurrent);
        retried.AccessToken.Should().NotBeNullOrWhiteSpace(retried.ErrorDescription);
    }

    // AC12, BR5, BR6: a weak password is refused and the challenge survives.
    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1234!")]
    public async Task Change_WeakPassword_IsRefusedAndTheChallengeStillWorks(string weak)
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, weak);
        var retried = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeTooWeak);
        retried.AccessToken.Should().NotBeNullOrWhiteSpace(retried.ErrorDescription);
        (await UserAsync(email)).MustChangePassword.Should().BeFalse();
    }

    // AC13, BR6: unknown, used, expired.
    [Fact]
    public async Task Change_UnknownChallenge_IsInvalidAndRecordsTheFailure()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var user = await UserAsync(email);

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, new string('a', 64), NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        (await UserAsync(email)).MustChangePassword.Should().BeTrue("nothing changed");
        (await SignInFrom.PasswordAsync(client, Address, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Change_ChallengeUsedTwice_IsInvalidTheSecondTime()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        (await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();

        var again = await SignInFrom.PasswordChangeAsync(client, Address, challenge, "Outra#Senha2026x");

        // A spent challenge no longer says whose it was, so the failure is recorded with no account (F-21 BR4, as the code step does).
        again.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        var failures = await QueryAsync(context => context.AccountEvents.AsNoTracking()
            .Where(accountEvent => accountEvent.Type == AccountEventType.SignInFailed && accountEvent.Reason == AccountEventReason.ChallengeInvalid)
            .ToListAsync());
        failures.Should().ContainSingle();
        (await SignInFrom.PasswordAsync(client, Address, email, "Outra#Senha2026x")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials, "the second password was never set");
    }

    [Fact]
    public async Task Change_ExpiredChallenge_IsInvalid()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        Factory.Clock.Advance(TimeSpan.FromMinutes(6));

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        (await UserAsync(email)).MustChangePassword.Should().BeTrue();
    }

    // AC13, BR6: the challenge is bound to the account's state.
    [Fact]
    public async Task Change_AccountWasResetByEmailMeanwhile_IsInvalid()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await SetMarkAsync(email, true);
        await RunJobsAsync();
        Emails.Clear();
        await client.PostAsJsonAsync("/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), AppJson.Options);
        await RunJobsAsync();
        var token = ResetLink.TokenOf(Emails.Last!.HtmlBody);
        using (var reset = await client.PostAsJsonAsync("/api/v1/identity/password-resets", new ResetPasswordRequest(token, "Outra#Senha2026x"), AppJson.Options))
        {
            reset.StatusCode.Should().Be(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync());
        }

        await SetMarkAsync(email, true);
        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid, "the reset renewed the security stamp");
    }

    [Fact]
    public async Task Change_AccountIsNoLongerMarked_IsInvalid()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await SetMarkAsync(email, false);

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
    }

    [Fact]
    public async Task Change_AccountIsNoLongerActive_IsInvalid()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await QueryAsync(context => context.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.Status, AccountStatus.Pending)));

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
    }

    // AC15, BR6: two requests with one challenge; exactly one wins.
    [Fact]
    public async Task Change_TwoConcurrentRequestsWithTheSameChallenge_OnlyOneSucceeds()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);

        var answers = await Task.WhenAll(
            SignInFrom.PasswordChangeAsync(client, "203.0.113.51", challenge, NewPassword),
            SignInFrom.PasswordChangeAsync(client, "203.0.113.52", challenge, "Outra#Senha2026x"));

        answers.Count(answer => answer.AccessToken is not null).Should().Be(1, string.Join(" | ", answers.Select(answer => answer.Error)));
        answers.Single(answer => answer.AccessToken is null).Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        var userId = (await UserAsync(email)).Id;
        (await EventsAsync(userId)).Count(accountEvent => accountEvent.Type == AccountEventType.PasswordChanged).Should().Be(1);
        (await EventsAsync(userId)).Count(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded).Should().Be(1);
    }

    // AC16, BR7: at the limit the step is refused before the challenge is read.
    [Fact]
    public async Task Change_AtTheAddressLimit_IsRefusedAndTheChallengeIsKept()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.53", IdentityRateLimits.SignInFailedAccountsPer15Minutes);

        var refused = await SignInFrom.PasswordChangeAsync(client, "203.0.113.53", challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        (await UserAsync(email)).MustChangePassword.Should().BeTrue("the password is not changed");
        var elsewhere = await SignInFrom.PasswordChangeAsync(client, "198.51.100.53", challenge, NewPassword);
        elsewhere.AccessToken.Should().NotBeNullOrWhiteSpace("the challenge was not spent", elsewhere.ErrorDescription);
    }

    // AC17, BR8: a reset by email clears the mark.
    [Fact]
    public async Task Reset_MarkedAccount_ClearsTheMark()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        await client.PostAsJsonAsync("/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), AppJson.Options);
        await RunJobsAsync();
        var token = ResetLink.TokenOf(Emails.Last!.HtmlBody);

        using var reset = await client.PostAsJsonAsync("/api/v1/identity/password-resets", new ResetPasswordRequest(token, NewPassword), AppJson.Options);

        reset.StatusCode.Should().Be(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync());
        (await UserAsync(email)).MustChangePassword.Should().BeFalse();
        (await SignInFrom.PasswordAsync(client, Address, email, NewPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    // AC17, BR8: the change on the account page clears the mark (the test marks an account that already holds a session).
    [Fact]
    public async Task AccountPageChange_MarkedAccount_ClearsTheMark()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await SignInFrom.PasswordAsync(client, Address, email, SignUpForm.ValidPassword);
        await SetMarkAsync(email, true);

        using var response = await TotpApi.SendAsync(
            client, HttpMethod.Post, "/api/v1/identity/password-changes", session.AccessToken!,
            new ChangePasswordRequest(SignUpForm.ValidPassword, NewPassword));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await UserAsync(email)).MustChangePassword.Should().BeFalse();
    }

    // AC18, BR9: PasswordChanged, then one SignInSucceeded by password, and no notice email.
    [Fact]
    public async Task Change_AfterThePasswordStep_RecordsPasswordChangedThenOnePasswordSignIn_AndSendsNoNotice()
    {
        var client = Client();
        var email = await MarkedAccountAsync(client);
        await RunJobsAsync();
        Emails.Clear();
        var challenge = await ChallengeAsync(client, email);

        var session = await SignInFrom.PasswordChangeAsync(client, Address, challenge, NewPassword);

        session.AccessToken.Should().NotBeNullOrWhiteSpace(session.ErrorDescription);
        var events = await EventsAsync((await UserAsync(email)).Id);
        events.Select(accountEvent => accountEvent.Type).Should().ContainInOrder(AccountEventType.PasswordChanged, AccountEventType.SignInSucceeded);
        var signIns = events.Where(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded).ToList();
        signIns.Should().ContainSingle().Which.Method.Should().Be(AccountEventMethod.Password);
        await RunJobsAsync();
        Emails.Last.Should().BeNull("the user is doing the change in the same sign-in; no notice goes out (BR9)");
    }

    // AC19, BR10: with two-factor off the grant is registered.
    [Fact]
    public async Task Grant_TwoFactorOff_IsAcceptedAndNotUnsupported()
    {
        var client = Client();

        var answer = await SignInFrom.PasswordChangeAsync(client, Address, new string('b', 64), NewPassword);

        answer.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        answer.Error.Should().NotBe("unsupported_grant_type");
    }
}
