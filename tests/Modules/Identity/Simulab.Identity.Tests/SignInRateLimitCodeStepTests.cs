using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-38 with two-factor and Google sign-in on: the code step counts the challenge's account (AC4, BR1, BR3), and
/// the Google step is never counted nor refused by the limit (AC5).
/// </summary>
public sealed class SignInRateLimitCodeStepTests : IdentityApiTests
{
    private const int Limit = IdentityRateLimits.SignInFailedAccountsPer15Minutes;

    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
        GoogleTokens.Configure(builder);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    private async Task<(string Email, string Secret)> EnrolledAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        using (await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)))
        {
        }

        // The enrolment code's step is spent (F-11 BR4); the next code comes from the next step.
        Factory.Clock.Advance(Step);
        return (email, enrolment.Secret);
    }

    private static async Task<string> ChallengeAsync(HttpClient client, string email)
    {
        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        step.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        return step.Challenge!;
    }

    private Task<int> AccountEventCountAsync() => QueryAsync(context => context.AccountEvents.CountAsync());

    // AC4, BR3: at the limit the code step is refused before the challenge is read, so the challenge is still good elsewhere.
    [Fact]
    public async Task Code_AtTheLimit_IsRefusedBeforeTheChallengeIsRead()
    {
        var client = Client();
        var (email, secret) = await EnrolledAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.31", Limit);
        var eventsBefore = await AccountEventCountAsync();

        var refused = await SignInFrom.CodeAsync(client, "203.0.113.31", challenge, TotpApi.CodeAt(secret, Now));

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        (await AccountEventCountAsync()).Should().Be(eventsBefore, "a refused attempt writes no account event");

        var elsewhere = await SignInFrom.CodeAsync(client, "198.51.100.31", challenge, TotpApi.CodeAt(secret, Now));
        elsewhere.AccessToken.Should().NotBeNullOrWhiteSpace(elsewhere.ErrorDescription);
    }

    // AC4, BR1: a wrong code counts the challenge's account as a name.
    [Fact]
    public async Task Code_AWrongCode_CountsTheChallengesAccountAsAName()
    {
        var client = Client();
        var (email, _) = await EnrolledAsync(client);
        var challenge = await ChallengeAsync(client, email);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.32", Limit - 1);

        var wrong = await SignInFrom.CodeAsync(client, "203.0.113.32", challenge, "000000");
        var next = await SignInFrom.PasswordAsync(client, "203.0.113.32", "next@exemplo.com", "not-the-password");

        wrong.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        next.Error.Should().Be(IdentityErrorCodes.SignInRateLimited, "the wrong code was the 30th name");
    }

    // AC4, BR1: an invalid or spent challenge names no account, so it adds nothing.
    [Fact]
    public async Task Code_InvalidChallenges_NeverAddAName()
    {
        var client = Client();
        for (var i = 0; i < Limit + 10; i++)
        {
            var answer = await SignInFrom.CodeAsync(client, "203.0.113.33", $"not-a-challenge-{i}", "123456");
            answer.Error.Should().Be(IdentityErrorCodes.TotpChallengeInvalid);
        }

        var password = await SignInFrom.PasswordAsync(client, "203.0.113.33", "ghost@exemplo.com", "not-the-password");

        password.Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
    }

    // BR1, BR5: a right code takes the account's own name out of the set.
    [Fact]
    public async Task Code_ARightCode_TakesTheAccountsNameOut()
    {
        var client = Client();
        var (email, secret) = await EnrolledAsync(client);
        (await SignInFrom.PasswordAsync(client, "203.0.113.34", email, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.34", Limit - 2);
        var challenge = await ChallengeAsync(client, email);

        var signedIn = await SignInFrom.CodeAsync(client, "203.0.113.34", challenge, TotpApi.CodeAt(secret, Now));

        // 29 names were in the set, the account's own among them; it left, so two new names fit.
        signedIn.AccessToken.Should().NotBeNullOrWhiteSpace(signedIn.ErrorDescription);
        (await SignInFrom.PasswordAsync(client, "203.0.113.34", "new-1@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.34", "new-2@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.34", "new-3@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
    }

    // AC5: at the limit the Google step is processed as before, and its failures never add a name.
    [Fact]
    public async Task Google_AtTheLimit_IsProcessedAsBeforeAndNeverCounts()
    {
        var client = Client();
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.35", Limit);
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), "ana.ribeiro@gmail.com", audience: "someone-else.apps.googleusercontent.com");

        var refused = await SignInFrom.GoogleAsync(client, "203.0.113.35", token);

        refused.Error.Should().Be(IdentityErrorCodes.GoogleTokenInvalid, "the limit does not refuse the Google step");

        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.36", Limit - 1);
        for (var i = 0; i < 5; i++)
        {
            (await SignInFrom.GoogleAsync(client, "203.0.113.36", token)).Error.Should().Be(IdentityErrorCodes.GoogleTokenInvalid);
        }

        var thirtieth = await SignInFrom.PasswordAsync(client, "203.0.113.36", "thirtieth@exemplo.com", "not-the-password");
        thirtieth.Error.Should().Be(IdentityErrorCodes.InvalidCredentials, "five Google failures added no name");
    }
}
