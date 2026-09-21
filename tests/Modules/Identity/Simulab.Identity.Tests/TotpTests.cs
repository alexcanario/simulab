using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>F-11 with the feature on: enrolment, the two-step sign-in, recovery codes and turning it off (AC1-AC12, AC15).</summary>
public sealed class TotpTests : IdentityApiTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    /// <summary>A signed-in account with two-factor on: its email, secret, recovery codes and session.</summary>
    private async Task<(string Email, string Secret, IReadOnlyList<string> Codes, TokenResponse Session)> EnrolledAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        var codes = await TotpApi.ReadAsync<RecoveryCodesResponse>(await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)));

        // The enrolment code's step is spent (BR4); the next code comes from the next step.
        Factory.Clock.Advance(Step);
        return (email, enrolment.Secret, codes.Codes, session);
    }

    private Task<User> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

    private Task<string?> RecoveryRowAsync(Guid userId) =>
        QueryAsync(context => context.UserTokens
            .Where(token => token.UserId == userId && token.LoginProvider == RecoveryCodes.TokenProvider && token.Name == RecoveryCodes.TokenName)
            .Select(token => token.Value)
            .SingleOrDefaultAsync());

    private static async Task<TokenResponse> SignInWithCodeAsync(HttpClient client, string email, string code)
    {
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        return await TotpApi.CodeStepAsync(client, step.Challenge, code);
    }

    // AC1.
    [Fact]
    public async Task Start_SignedInUser_ReturnsSecretUriAndQrCodeWithTwoFactorStillOff()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);

        enrolment.Secret.Should().HaveLength(32);
        enrolment.OtpAuthUri.Should().StartWith("otpauth://totp/Simulab%3A")
            .And.Contain(Uri.EscapeDataString(email))
            .And.Contain($"secret={enrolment.Secret}");
        enrolment.QrCodeDataUri.Should().StartWith("data:image/png;base64,");
        (await TotpApi.StatusAsync(client, session.AccessToken!)).Enabled.Should().BeFalse();
        (await UserAsync(email)).TwoFactorEnabled.Should().BeFalse();
    }

    // AC2.
    [Fact]
    public async Task Confirm_CodeFromTheSecret_TurnsItOnWithEncryptedSecretAndTenHashedRecoveryCodes()
    {
        var client = Client();
        var (email, secret, codes, session) = await EnrolledAsync(client);

        var user = await UserAsync(email);
        user.TwoFactorEnabled.Should().BeTrue();
        user.TotpEnabledAt.Should().NotBeNull();
        user.TotpSecretEncrypted.Should().NotBeNullOrEmpty().And.NotContain(secret);

        codes.Should().HaveCount(10).And.OnlyHaveUniqueItems();
        var row = await RecoveryRowAsync(user.Id);
        row.Should().NotBeNull();
        foreach (var code in codes)
        {
            row.Should().NotContain(code).And.NotContain(RecoveryCodes.Normalize(code));
        }

        var status = await TotpApi.StatusAsync(client, session.AccessToken!);
        status.Enabled.Should().BeTrue();
        status.RecoveryCodesLeft.Should().Be(10);
    }

    // AC3.
    [Fact]
    public async Task Confirm_WrongCode_RefusesKeepsItOffAndCountsAFailure()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        await TotpApi.StartAsync(client, session.AccessToken!);

        using var response = await TotpApi.ConfirmAsync(client, session.AccessToken!, "000000");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        var user = await UserAsync(email);
        user.TwoFactorEnabled.Should().BeFalse();
        user.AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Start_WhenAlreadyOn_ReturnsAlreadyEnabled()
    {
        var client = Client();
        var (_, _, _, session) = await EnrolledAsync(client);

        using var response = await TotpApi.SendAsync(client, HttpMethod.Post, TotpApi.Route + "/enrolments", session.AccessToken!);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.TotpAlreadyEnabled);
    }

    // AC4.
    [Fact]
    public async Task SignIn_TwoFactorOn_PasswordGivesAChallengeAndTheCodeGivesTheTokens()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);

        var passwordStep = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);

        passwordStep.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        passwordStep.AccessToken.Should().BeNull();
        passwordStep.Challenge.Should().NotBeNullOrEmpty();
        passwordStep.ExpiresIn.Should().Be(300);

        var tokens = await TotpApi.CodeStepAsync(client, passwordStep.Challenge, TotpApi.CodeAt(secret, Now));

        tokens.Error.Should().BeNull(tokens.ErrorDescription);
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
    }

    // AC5.
    [Fact]
    public async Task CodeStep_ChallengeUsedTwice_IsRefused()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);
        var passwordStep = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);

        var wrong = await TotpApi.CodeStepAsync(client, passwordStep.Challenge, "000000");
        var again = await TotpApi.CodeStepAsync(client, passwordStep.Challenge, TotpApi.CodeAt(secret, Now));

        // BR9: the challenge was spent by the wrong code, so even the right one cannot use it.
        wrong.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        again.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        again.AccessToken.Should().BeNull();
    }

    // AC5.
    [Fact]
    public async Task CodeStep_ChallengeOlderThanFiveMinutes_IsRefused()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);
        var passwordStep = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);

        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var tokens = await TotpApi.CodeStepAsync(client, passwordStep.Challenge, TotpApi.CodeAt(secret, Now));

        tokens.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        tokens.AccessToken.Should().BeNull();
    }

    // AC5.
    [Fact]
    public async Task CodeStep_AnotherAccountsCodeOrAnUnknownChallenge_IsRefused()
    {
        var client = Client();
        var (email, _, _, _) = await EnrolledAsync(client);
        var (_, otherSecret, _, _) = await EnrolledAsync(client);
        var passwordStep = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);

        var otherCode = await TotpApi.CodeStepAsync(client, passwordStep.Challenge, TotpApi.CodeAt(otherSecret, Now));
        var unknown = await TotpApi.CodeStepAsync(client, new string('a', 64), TotpApi.CodeAt(otherSecret, Now));
        var missing = await TotpApi.CodeStepAsync(client, null, TotpApi.CodeAt(otherSecret, Now));

        otherCode.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        otherCode.AccessToken.Should().BeNull();
        unknown.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        missing.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
    }

    // AC6.
    [Fact]
    public async Task CodeStep_CodeAlreadyAccepted_IsRefusedInsideItsWindow()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);
        var code = TotpApi.CodeAt(secret, Now);

        var first = await SignInWithCodeAsync(client, email, code);
        var replay = await SignInWithCodeAsync(client, email, code);

        first.AccessToken.Should().NotBeNullOrEmpty(first.ErrorDescription);
        replay.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        replay.AccessToken.Should().BeNull();
    }

    // AC7.
    [Fact]
    public async Task CodeStep_CodeFromThePreviousOrTheNextStep_IsAccepted()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);

        // The next step first, then two steps on, so the previous step of that moment is still unused.
        var next = await SignInWithCodeAsync(client, email, TotpApi.CodeAt(secret, Now + Step));
        Factory.Clock.Advance(Step * 3);
        var previous = await SignInWithCodeAsync(client, email, TotpApi.CodeAt(secret, Now - Step));
        var tooOld = await SignInWithCodeAsync(client, email, TotpApi.CodeAt(secret, Now - (Step * 3)));

        next.AccessToken.Should().NotBeNullOrEmpty(next.ErrorDescription);
        previous.AccessToken.Should().NotBeNullOrEmpty(previous.ErrorDescription);
        tooOld.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
    }

    // AC8.
    [Fact]
    public async Task CodeStep_RecoveryCode_SignsInOnceAndLeavesNine()
    {
        var client = Client();
        var (email, _, codes, _) = await EnrolledAsync(client);

        var first = await SignInWithCodeAsync(client, email, codes[0].ToLowerInvariant());
        var again = await SignInWithCodeAsync(client, email, codes[0]);

        first.AccessToken.Should().NotBeNullOrEmpty(first.ErrorDescription);
        again.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        (await TotpApi.StatusAsync(client, first.AccessToken!)).RecoveryCodesLeft.Should().Be(9);
    }

    // AC9.
    [Fact]
    public async Task CodeStep_FiveWrongCodes_LockTheAccountAndRefuseTheRightCodeWithTheSecondsLeft()
    {
        var client = Client();
        var (email, secret, _, _) = await EnrolledAsync(client);

        // Each code needs the password again, and a right password must not clear the count (BR10).
        for (var attempt = 0; attempt < 4; attempt++)
        {
            (await SignInWithCodeAsync(client, email, "000000")).Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        }

        var lastChallenge = (await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword)).Challenge;
        var spareChallenge = (await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword)).Challenge;
        (await TotpApi.CodeStepAsync(client, lastChallenge, "000000")).Error.Should().Be(IdentityErrorCodes.AccountLocked);

        var rightCode = await TotpApi.CodeStepAsync(client, spareChallenge, TotpApi.CodeAt(secret, Now));

        rightCode.Error.Should().Be(IdentityErrorCodes.AccountLocked);
        int.Parse(rightCode.ErrorDescription!, System.Globalization.CultureInfo.InvariantCulture).Should().BePositive();
        rightCode.AccessToken.Should().BeNull();
        (await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);
    }

    // AC10.
    [Fact]
    public async Task Regenerate_ValidCode_ReturnsTenNewCodesAndTheOldOnesStopWorking()
    {
        var client = Client();
        var (email, secret, oldCodes, session) = await EnrolledAsync(client);

        var fresh = await TotpApi.ReadAsync<RecoveryCodesResponse>(await TotpApi.RegenerateAsync(client, session.AccessToken!, TotpApi.CodeAt(secret, Now)));

        fresh.Codes.Should().HaveCount(10).And.NotIntersectWith(oldCodes);
        (await SignInWithCodeAsync(client, email, oldCodes[1])).Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        (await SignInWithCodeAsync(client, email, fresh.Codes[1])).AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Regenerate_WrongCode_KeepsTheOldCodes()
    {
        var client = Client();
        var (email, _, oldCodes, session) = await EnrolledAsync(client);

        using var response = await TotpApi.RegenerateAsync(client, session.AccessToken!, "000000");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await SignInWithCodeAsync(client, email, oldCodes[2])).AccessToken.Should().NotBeNullOrEmpty();
    }

    // AC11.
    [Fact]
    public async Task Disable_PasswordAndCode_ClearsEverythingAndThePasswordAloneSignsIn()
    {
        var client = Client();
        var (email, secret, _, session) = await EnrolledAsync(client);

        using var response = await TotpApi.DisableAsync(client, session.AccessToken!, SignUpForm.ValidPassword, TotpApi.CodeAt(secret, Now));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var user = await UserAsync(email);
        user.TwoFactorEnabled.Should().BeFalse();
        user.TotpSecretEncrypted.Should().BeNull();
        user.TotpEnabledAt.Should().BeNull();
        user.TotpLastAcceptedStep.Should().BeNull();
        (await RecoveryRowAsync(user.Id)).Should().BeNull();

        var signIn = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        signIn.AccessToken.Should().NotBeNullOrEmpty(signIn.ErrorDescription);
    }

    // AC11.
    [Fact]
    public async Task Disable_WrongPasswordOrWrongCode_ChangesNothing()
    {
        var client = Client();
        var (email, secret, _, session) = await EnrolledAsync(client);

        using var wrongPassword = await TotpApi.DisableAsync(client, session.AccessToken!, "not-the-password", TotpApi.CodeAt(secret, Now));
        using var wrongCode = await TotpApi.DisableAsync(client, session.AccessToken!, SignUpForm.ValidPassword, "000000");

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await wrongPassword.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.TotpCurrentPasswordInvalid);
        wrongCode.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await wrongCode.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.TotpCodeInvalid);

        var user = await UserAsync(email);
        user.TwoFactorEnabled.Should().BeTrue();
        user.TotpSecretEncrypted.Should().NotBeNull();
        (await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.TotpRequired);
    }

    // AC11: a recovery code is accepted to turn it off (BR6).
    [Fact]
    public async Task Disable_WithARecoveryCode_TurnsItOff()
    {
        var client = Client();
        var (email, _, codes, session) = await EnrolledAsync(client);

        using var response = await TotpApi.DisableAsync(client, session.AccessToken!, SignUpForm.ValidPassword, codes[3]);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await UserAsync(email)).TwoFactorEnabled.Should().BeFalse();
    }

    // AC12.
    [Fact]
    public async Task TurningOnAndOff_OtherSessionsKeepWorking()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 2);
        var enrolment = await TotpApi.StartAsync(client, sessions[0].AccessToken!);
        using (await TotpApi.ConfirmAsync(client, sessions[0].AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)))
        {
        }

        var afterOn = await TokenClient.RefreshAsync(client, sessions[1].RefreshToken!);
        afterOn.AccessToken.Should().NotBeNullOrEmpty(afterOn.ErrorDescription);

        Factory.Clock.Advance(Step);
        using (await TotpApi.DisableAsync(client, sessions[0].AccessToken!, SignUpForm.ValidPassword, TotpApi.CodeAt(enrolment.Secret, Now)))
        {
        }

        var afterOff = await TokenClient.RefreshAsync(client, afterOn.RefreshToken!);
        afterOff.AccessToken.Should().NotBeNullOrEmpty(afterOff.ErrorDescription);
        (await SignedInSessions.StatusOfAsync(client, sessions[0])).Should().Be(HttpStatusCode.OK);
    }

    // AC15.
    [Fact]
    public async Task EraseAccount_TwoFactorOn_RemovesTheSecretTheFlagAndTheRecoveryCodes()
    {
        var client = Client();
        var (email, _, _, session) = await EnrolledAsync(client);
        var userId = (await UserAsync(email)).Id;

        using var response = await TotpApi.SendAsync(
            client, HttpMethod.Post, "/api/v1/identity/account-erasures", session.AccessToken!, new EraseAccountRequest(SignUpForm.ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var row = await QueryAsync(context => context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(user => user.Id == userId));
        row.TwoFactorEnabled.Should().BeFalse();
        row.TotpSecretEncrypted.Should().BeNull();
        row.TotpEnabledAt.Should().BeNull();
        (await RecoveryRowAsync(userId)).Should().BeNull();
    }

    [Fact]
    public async Task Routes_WithoutASignedInCaller_Answer401()
    {
        using var response = await Client().GetAsync(TotpApi.Route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenApiDocument_WithTheFeatureOn_Returns200()
    {
        using var response = await Client().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("/api/v1/identity/totp/recovery-codes");
    }
}
