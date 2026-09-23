using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>F-20 with two-factor on too: the Google step leads to the same code step as a password (AC10, AC16).</summary>
public sealed class GoogleSignInTwoFactorTests : IdentityApiTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
        GoogleTokens.Configure(builder);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    private Task<User> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleAsync(user => user.Email == email));

    /// <summary>An active password account with two-factor on, and its authenticator secret.</summary>
    private async Task<(string Email, string Secret)> EnrolledAsync(HttpClient client)
    {
        var email = await ActiveUser.CreateAsync(client, Factory, $"ana.{Guid.CreateVersion7():N}@gmail.com");
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        using (await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)))
        {
        }

        Factory.Clock.Advance(Step);
        return (email, enrolment.Secret);
    }

    // AC10.
    [Fact]
    public async Task Grant_AccountWithTwoFactor_AsksForTheCodeAndTheCodeStepSignsIn()
    {
        var client = Client();
        var (email, secret) = await EnrolledAsync(client);

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(GoogleTokens.NewSubject(), email));

        grant.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        grant.AccessToken.Should().BeNull();
        grant.Challenge.Should().NotBeNullOrEmpty();
        var tokens = await TotpApi.CodeStepAsync(client, grant.Challenge, TotpApi.CodeAt(secret, Now));
        tokens.AccessToken.Should().NotBeNull(tokens.Error);
    }

    // AC10 with change note v2: the Google step leaves the count and the lockout for the code step.
    [Fact]
    public async Task Grant_LockedAccountWithTwoFactor_LeavesTheLockoutAndTheCodeStepRefuses()
    {
        var client = Client();
        var (email, secret) = await EnrolledAsync(client);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "Wrong#Password1");
        }

        var locked = await UserAsync(email);
        locked.LockoutEnd.Should().NotBeNull();

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(GoogleTokens.NewSubject(), email));

        grant.Error.Should().Be(IdentityErrorCodes.TotpRequired);
        var after = await UserAsync(email);
        after.AccessFailedCount.Should().Be(locked.AccessFailedCount);
        after.LockoutEnd.Should().Be(locked.LockoutEnd);
        var code = await TotpApi.CodeStepAsync(client, grant.Challenge, TotpApi.CodeAt(secret, Now));
        code.AccessToken.Should().BeNull();
        code.Error.Should().Be(IdentityErrorCodes.AccountLocked);
    }

    // AC16: turning two-factor off asks for the current password too.
    [Fact]
    public async Task Disable_AccountWithoutPassword_AnswersPasswordNotSet()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = $"ana.{Guid.CreateVersion7():N}@gmail.com";
        await PostAsync(client, "/api/v1/identity/google-registrations", GoogleTokens.Registration(GoogleTokens.Issue(subject, email)), HttpStatusCode.Created);
        var session = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        using (await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)))
        {
        }

        Factory.Clock.Advance(Step);

        using var response = await TotpApi.DisableAsync(client, session.AccessToken!, "anything", TotpApi.CodeAt(enrolment.Secret, Now));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.PasswordNotSet);
        (await UserAsync(email)).TwoFactorEnabled.Should().BeTrue();
    }
}
