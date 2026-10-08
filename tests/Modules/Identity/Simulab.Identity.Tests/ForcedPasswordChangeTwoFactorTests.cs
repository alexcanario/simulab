using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-53 with two-factor on: a marked account still proves both factors first (AC8), the two challenges are kinds of
/// their own (AC14), and the sign-in the change step records names the last factor (AC18).
/// </summary>
public sealed class ForcedPasswordChangeTwoFactorTests : IdentityApiTests
{
    private const string NewPassword = "Revisar#2026!x";
    private const string Address = "203.0.113.60";
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    /// <summary>An account with two-factor on, already marked.</summary>
    private async Task<(string Email, string Secret, IReadOnlyList<string> Codes)> MarkedWithTwoFactorAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        var codes = await TotpApi.ReadAsync<RecoveryCodesResponse>(
            await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)));

        // The enrolment code's step is spent (F-11 BR4); the next code comes from the next step.
        Factory.Clock.Advance(Step);
        await QueryAsync(context => context.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.MustChangePassword, true)));

        // The sign-in that enrolled the account is set-up, not the subject: the tests read what comes after it.
        _setupEvents = await QueryAsync(context => context.AccountEvents.AsNoTracking().Select(accountEvent => accountEvent.Id).ToListAsync());
        return (email, enrolment.Secret, codes.Codes);
    }

    private List<Guid> _setupEvents = [];

    private Task<User> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

    private async Task<List<AccountEvent>> EventsAsync(Guid userId) =>
        await QueryAsync(context => context.AccountEvents.AsNoTracking()
            .Where(accountEvent => accountEvent.UserId == userId && !_setupEvents.Contains(accountEvent.Id))
            .OrderBy(accountEvent => accountEvent.CreatedAt).ThenBy(accountEvent => accountEvent.Id)
            .ToListAsync());

    // AC8, BR3: the code step hands out the password-change challenge, not tokens, and is not yet a sign-in.
    [Fact]
    public async Task Code_MarkedAccount_AnswersThePasswordChangeChallengeAndRecordsNoSignIn()
    {
        var client = Client();
        var (email, secret, _) = await MarkedWithTwoFactorAsync(client);
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.TotpRequired, "two-factor comes before the change");

        var code = await SignInFrom.CodeAsync(client, Address, step.Challenge, TotpApi.CodeAt(secret, Now));

        code.Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);
        code.Challenge.Should().NotBeNullOrWhiteSpace();
        code.AccessToken.Should().BeNull();
        (await EventsAsync((await UserAsync(email)).Id)).Should().NotContain(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded);
    }

    // AC18, BR9: after the app code the sign-in is recorded once, by that code.
    [Fact]
    public async Task Change_AfterTheAppCode_RecordsOneSignInByTheAppCode()
    {
        var client = Client();
        var (email, secret, _) = await MarkedWithTwoFactorAsync(client);
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var code = await SignInFrom.CodeAsync(client, Address, step.Challenge, TotpApi.CodeAt(secret, Now));

        var session = await SignInFrom.PasswordChangeAsync(client, Address, code.Challenge, NewPassword);

        session.AccessToken.Should().NotBeNullOrWhiteSpace(session.ErrorDescription);
        var events = await EventsAsync((await UserAsync(email)).Id);
        events.Select(accountEvent => accountEvent.Type).Should().ContainInOrder(AccountEventType.PasswordChanged, AccountEventType.SignInSucceeded);
        events.Where(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded).Should().ContainSingle()
            .Which.Method.Should().Be(AccountEventMethod.TotpCode);
        (await UserAsync(email)).MustChangePassword.Should().BeFalse();
    }

    // AC18, BR9: after a recovery code, by that code.
    [Fact]
    public async Task Change_AfterARecoveryCode_RecordsOneSignInByTheRecoveryCode()
    {
        var client = Client();
        var (email, _, codes) = await MarkedWithTwoFactorAsync(client);
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var code = await SignInFrom.CodeAsync(client, Address, step.Challenge, codes[0]);
        code.Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);

        var session = await SignInFrom.PasswordChangeAsync(client, Address, code.Challenge, NewPassword);

        session.AccessToken.Should().NotBeNullOrWhiteSpace(session.ErrorDescription);
        (await EventsAsync((await UserAsync(email)).Id)).Where(accountEvent => accountEvent.Type == AccountEventType.SignInSucceeded)
            .Should().ContainSingle().Which.Method.Should().Be(AccountEventMethod.RecoveryCode);
    }

    // AC14, BR6: a two-factor challenge never stands in for a password-change one.
    [Fact]
    public async Task Change_WithATwoFactorChallenge_IsInvalid()
    {
        var client = Client();
        var (email, _, _) = await MarkedWithTwoFactorAsync(client);
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.TotpRequired);

        var refused = await SignInFrom.PasswordChangeAsync(client, Address, step.Challenge, NewPassword);

        refused.Error.Should().Be(IdentityErrorCodes.PasswordChangeChallengeInvalid);
        (await UserAsync(email)).MustChangePassword.Should().BeTrue("the password did not change");
    }

    // AC14, BR6: and a password-change challenge never stands in for a two-factor one.
    [Fact]
    public async Task Code_WithAPasswordChangeChallenge_IsInvalid()
    {
        var client = Client();
        var (email, secret, _) = await MarkedWithTwoFactorAsync(client);
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var code = await SignInFrom.CodeAsync(client, Address, step.Challenge, TotpApi.CodeAt(secret, Now));
        code.Error.Should().Be(IdentityErrorCodes.PasswordChangeRequired);
        Factory.Clock.Advance(Step);

        var refused = await SignInFrom.CodeAsync(client, Address, code.Challenge, TotpApi.CodeAt(secret, Now));

        refused.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        refused.AccessToken.Should().BeNull();
    }
}
