using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-21 with two-factor on: the password step is not yet a sign-in (BR3), the code step is, and it says which
/// of the two codes was used. Turning two-factor on and off and regenerating the codes are events of their own.
/// </summary>
public sealed class AccountEventTwoFactorTests : IdentityApiTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);
    private const string AccountEvents = "/api/v1/identity/account-events";

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    // AC1, AC6: the password step records nothing; the app code signs in and turning it on was its own event.
    [Fact]
    public async Task SignIn_ThroughTheAppCode_RecordsOneSignInWithThatMethodAndNoneForThePasswordStep()
    {
        var client = Client();
        var (email, secret, _) = await EnrolledAsync(client);
        var userId = await IdOfAsync(email);
        var beforeSignIn = await EventsAsync(userId);

        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        var afterPassword = await EventsAsync(userId);

        var session = await TotpApi.CodeStepAsync(client, step.Challenge, TotpApi.CodeAt(secret, Now));

        session.AccessToken.Should().NotBeNull(session.ErrorDescription);
        afterPassword.Count.Should().Be(beforeSignIn.Count, "the password step is not yet a sign-in");
        var signIn = (await EventsAsync(userId))[0];
        signIn.Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        signIn.Method.Should().Be(nameof(AccountEventMethod.TotpCode));

        // The enrolment that set this account up is in the trail too (AC6).
        beforeSignIn.Select(entry => entry.Event).Should().Contain(nameof(AccountEventType.TwoFactorEnabled));
    }

    // AC1: a recovery code signs in, and says so.
    [Fact]
    public async Task SignIn_WithARecoveryCode_RecordsThatMethod()
    {
        var client = Client();
        var (email, _, codes) = await EnrolledAsync(client);
        var userId = await IdOfAsync(email);

        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var session = await TotpApi.CodeStepAsync(client, step.Challenge, codes[0]);

        session.AccessToken.Should().NotBeNull(session.ErrorDescription);
        var signIn = (await EventsAsync(userId))[0];
        signIn.Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        signIn.Method.Should().Be(nameof(AccountEventMethod.RecoveryCode));
    }

    // AC3: a wrong code and a spent challenge, each with its reason.
    [Fact]
    public async Task CodeStep_WrongCodeAndSpentChallenge_RecordTheirReasons()
    {
        var client = Client();
        var (email, _, _) = await EnrolledAsync(client);
        var userId = await IdOfAsync(email);

        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var wrong = await TotpApi.CodeStepAsync(client, step.Challenge, "000000");
        var spent = await TotpApi.CodeStepAsync(client, step.Challenge, "000000");

        wrong.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        spent.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        var failures = (await EventsAsync(userId)).Where(entry => entry.Event == nameof(AccountEventType.SignInFailed)).ToList();
        failures.Should().ContainSingle(entry => entry.Reason == nameof(AccountEventReason.WrongCode));

        // The spent challenge names no account, so it is listed with none.
        var unnamed = (await ListAsync($"?event={nameof(AccountEventType.SignInFailed)}")).Items
            .Where(entry => entry.Account is null)
            .ToList();
        unnamed.Should().ContainSingle().Which.Reason.Should().Be(nameof(AccountEventReason.ChallengeInvalid));
    }

    // AC6: new recovery codes and turning two-factor off.
    [Fact]
    public async Task RecoveryCodesRegeneratedAndTwoFactorTurnedOff_EachRecordTheirOwnEvent()
    {
        var client = Client();
        var (email, secret, _) = await EnrolledAsync(client);
        var userId = await IdOfAsync(email);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        session.AccessToken.Should().BeNull("two-factor is on, so the password alone issues no token");

        var signedIn = await SignedInWithCodeAsync(client, email, secret);

        // The sign-in spent this step's code (F-11 BR4); the next action needs the next step.
        Factory.Clock.Advance(Step);
        var regenerated = await TotpApi.RegenerateAsync(client, signedIn, TotpApi.CodeAt(secret, Now));
        regenerated.IsSuccessStatusCode.Should().BeTrue(await regenerated.Content.ReadAsStringAsync());

        Factory.Clock.Advance(Step);
        var disabled = await TotpApi.DisableAsync(client, signedIn, SignUpForm.ValidPassword, TotpApi.CodeAt(secret, Now));
        disabled.IsSuccessStatusCode.Should().BeTrue(await disabled.Content.ReadAsStringAsync());

        (await EventsAsync(userId)).Select(entry => entry.Event).Should().Contain(
        [
            nameof(AccountEventType.TwoFactorEnabled),
            nameof(AccountEventType.RecoveryCodesRegenerated),
            nameof(AccountEventType.TwoFactorDisabled)
        ]);
    }

    private async Task<(string Email, string Secret, IReadOnlyList<string> Codes)> EnrolledAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        var codes = await TotpApi.ReadAsync<RecoveryCodesResponse>(
            await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)));

        // The enrolment code's step is spent (F-11 BR4); the next code comes from the next step.
        Factory.Clock.Advance(Step);
        return (email, enrolment.Secret, codes.Codes);
    }

    private async Task<string> SignedInWithCodeAsync(HttpClient client, string email, string secret)
    {
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var session = await TotpApi.CodeStepAsync(client, step.Challenge, TotpApi.CodeAt(secret, Now));
        session.AccessToken.Should().NotBeNull(session.ErrorDescription);
        return session.AccessToken!;
    }

    private Task<Guid> IdOfAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().Where(user => user.Email == email).Select(user => user.Id).SingleAsync());

    private async Task<IReadOnlyList<AccountEventResponse>> EventsAsync(Guid userId) =>
        (await ListAsync($"?user={userId}&pageSize=100")).Items;

    private async Task<AccountEventPageResponse> ListAsync(string query)
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var client = await Accounts.SignedInAsync(Client(), admin.Email!);
        var response = await client.GetAsync(AccountEvents + query);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AccountEventPageResponse>(AppJson.Options))!;
    }
}
