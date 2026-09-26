using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-20 with the feature on and two-factor off: the <c>google</c> grant and the confirmation (AC2-AC9, AC11-AC13, AC16, AC17).</summary>
public sealed class GoogleSignInTests : IdentityApiTests
{
    private const string RegistrationRoute = "/api/v1/identity/google-registrations";

    protected override void ConfigureHost(IWebHostBuilder builder) => GoogleTokens.Configure(builder);

    private static string NewEmail() => $"ana.{Guid.CreateVersion7():N}@gmail.com";

    private Task<User?> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email));

    private Task<List<string>> GoogleKeysAsync(Guid userId) =>
        QueryAsync(context => context.UserLogins
            .Where(login => login.UserId == userId && login.LoginProvider == GoogleSignInProtocol.LoginProvider)
            .Select(login => login.ProviderKey)
            .ToListAsync());

    private static async Task<string> RegisterAsync(HttpClient client, string subject, string email, string? fullName = "Ana Ribeiro")
    {
        await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(GoogleTokens.Issue(subject, email), fullName), HttpStatusCode.Created);
        return email;
    }

    // AC2.
    [Theory]
    [InlineData("audience")]
    [InlineData("issuer")]
    [InlineData("signature")]
    [InlineData("expired")]
    public async Task Grant_TokenFailingACheck_IsRefusedAsInvalid(string broken)
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = NewEmail();
        var token = broken switch
        {
            "audience" => GoogleTokens.Issue(subject, email, audience: "someone-else.apps.googleusercontent.com"),
            "issuer" => GoogleTokens.Issue(subject, email, issuer: "https://accounts.example.com"),
            "signature" => GoogleTokens.Issue(subject, email, foreignKey: true),
            _ => GoogleTokens.Issue(subject, email, expires: DateTimeOffset.UtcNow.AddMinutes(-10)),
        };

        var grant = await GoogleTokens.GrantAsync(client, token);
        var registration = await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(token), HttpStatusCode.BadRequest);

        grant.Error.Should().Be(IdentityErrorCodes.GoogleTokenInvalid);
        grant.AccessToken.Should().BeNull();
        CodeOf(registration).Should().Be(IdentityErrorCodes.GoogleTokenInvalid);
        (await UserAsync(email)).Should().BeNull();
    }

    // AC2.
    [Fact]
    public async Task Grant_NoTokenAtAll_IsRefusedAsInvalid()
    {
        (await GoogleTokens.GrantAsync(Client(), null)).Error.Should().Be(IdentityErrorCodes.GoogleTokenInvalid);
    }

    // AC2.
    [Fact]
    public async Task Grant_EmailNotVerifiedByGoogle_IsRefused()
    {
        var client = Client();
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail(), emailVerified: false);

        var grant = await GoogleTokens.GrantAsync(client, token);
        var registration = await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(token), HttpStatusCode.UnprocessableEntity);

        grant.Error.Should().Be(IdentityErrorCodes.GoogleEmailNotVerified);
        CodeOf(registration).Should().Be(IdentityErrorCodes.GoogleEmailNotVerified);
    }

    // AC3.
    [Fact]
    public async Task Grant_NoAccount_AsksForSignUpWithTheGoogleIdentityAndCreatesNothing()
    {
        var email = NewEmail();

        var grant = await GoogleTokens.GrantAsync(Client(), GoogleTokens.Issue(GoogleTokens.NewSubject(), email, name: "Ana Google"));

        grant.Error.Should().Be(IdentityErrorCodes.GoogleSignUpRequired);
        grant.Email.Should().Be(email);
        grant.Name.Should().Be("Ana Google");
        grant.AccessToken.Should().BeNull();
        (await UserAsync(email)).Should().BeNull();
    }

    // AC4.
    [Fact]
    public async Task Register_ValidTokenAndConsents_CreatesAnActiveLinkedAccountWithoutPasswordOrEmail()
    {
        var client = Client("pt-BR");
        var subject = GoogleTokens.NewSubject();
        var email = NewEmail();

        await RegisterAsync(client, subject, email, fullName: "  Ana Google  ");
        await RunJobsAsync();

        var user = (await UserAsync(email))!;
        user.Status.Should().Be(AccountStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        user.PasswordHash.Should().BeNull();
        user.FullName.Should().Be("Ana Google");
        user.PreferredLanguage.Should().Be("pt-BR");
        (await GoogleKeysAsync(user.Id)).Should().Equal(subject);
        var consent = await QueryAsync(context => context.ConsentRecords.AsNoTracking().SingleAsync(record => record.UserId == user.Id));
        consent.TermsVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.PrivacyVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.DeclaresAdult.Should().BeTrue();
        Emails.Count.Should().Be(0, "Google proved the address, so no verification email is sent");

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));
        grant.AccessToken.Should().NotBeNull(grant.Error);
        grant.RefreshToken.Should().NotBeNull();
    }

    // AC5.
    [Theory]
    [InlineData("adult", IdentityErrorCodes.AgeDeclarationRequired)]
    [InlineData("terms", IdentityErrorCodes.ConsentRequired)]
    [InlineData("privacy", IdentityErrorCodes.ConsentRequired)]
    [InlineData("version", IdentityErrorCodes.TermsVersionOutdated)]
    public async Task Register_MissingConsentOrOutdatedVersion_AnswersTheSignUpCodeAndCreatesNothing(string missing, string code)
    {
        var email = NewEmail();
        var valid = GoogleTokens.Registration(GoogleTokens.Issue(GoogleTokens.NewSubject(), email));
        var request = missing switch
        {
            "adult" => valid with { DeclaresAdult = false },
            "terms" => valid with { AcceptsTerms = false },
            "privacy" => valid with { AcceptsPrivacy = false },
            _ => valid with { TermsVersion = "2025-v0" },
        };

        var body = await PostAsync(Client(), RegistrationRoute, request, missing == "version" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest);

        CodeOf(body).Should().Be(code);
        (await UserAsync(email)).Should().BeNull();
    }

    // AC6.
    [Fact]
    public async Task Grant_LinkedAccount_SignsInEvenAfterTheAddressChanged()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = await RegisterAsync(client, subject, NewEmail());
        var user = (await UserAsync(email))!;
        var newEmail = NewEmail();
        await QueryAsync(context => context.Users.Where(row => row.Id == user.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(row => row.Email, newEmail)
            .SetProperty(row => row.NormalizedEmail, newEmail.ToUpperInvariant())));

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, $"other.{Guid.CreateVersion7():N}@gmail.com"));

        grant.AccessToken.Should().NotBeNull(grant.Error);
    }

    // F-29 AC16 (was F-20 AC7): an active account is no longer linked by its address. With the explicit link
    // on the Security page, linking on the way in would silently undo a disconnection at the next sign-in.
    [Fact]
    public async Task Grant_ActivePasswordAccountWithTheSameGmailAddress_IsRefusedAndLinksNothing()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, NewEmail());
        var subject = GoogleTokens.NewSubject();

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        grant.AccessToken.Should().BeNull();
        grant.Error.Should().Be(IdentityErrorCodes.GoogleAccountExists);
        (await GoogleKeysAsync((await UserAsync(email))!.Id)).Should().BeEmpty("F-29 BR11: nothing is linked here");
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull("the password keeps working");
    }

    // AC8, re-founded for F-29 BR11: the account reaches the grant through its **link**, not through its
    // address, because the address no longer links anything. What the criterion says is unchanged: a Google
    // sign-in clears a lockout the password attempts caused.
    [Fact]
    public async Task Grant_LinkedAccountLockedByWrongPasswords_SignsInAndClearsTheLockout()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, NewEmail());
        var subject = GoogleTokens.NewSubject();
        await LinkAsync(email, subject);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "Wrong#Password1");
        }

        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        grant.AccessToken.Should().NotBeNull(grant.Error);
        var user = (await UserAsync(email))!;
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
    }

    /// <summary>F-29: the link an account gets on the Security page, written straight into the table.</summary>
    private async Task LinkAsync(string email, string subject)
    {
        var user = (await UserAsync(email))!;
        await QueryAsync(async context =>
        {
            context.UserLogins.Add(new Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>
            {
                UserId = user.Id,
                LoginProvider = GoogleSignInProtocol.LoginProvider,
                ProviderKey = subject,
                ProviderDisplayName = email
            });
            return await context.SaveChangesAsync();
        });
    }

    // AC9, change note v3: the grant sends a pending account to the confirmation, which takes it over.
    [Fact]
    public async Task Grant_PendingGmailAccount_AsksForTheConfirmationWhichTakesTheAccountOver()
    {
        var client = Client();
        var email = NewEmail();
        await PostAsync(client, "/api/v1/identity/registrations", SignUpForm.Valid(email) with { FullName = "Someone Else" }, HttpStatusCode.Accepted);
        var before = (await UserAsync(email))!;
        before.Status.Should().Be(AccountStatus.Pending);
        var subject = GoogleTokens.NewSubject();
        var token = GoogleTokens.Issue(subject, email);

        var grant = await GoogleTokens.GrantAsync(client, token);

        grant.Error.Should().Be(IdentityErrorCodes.GoogleSignUpRequired);
        var untouched = (await UserAsync(email))!;
        untouched.Status.Should().Be(AccountStatus.Pending);
        untouched.PasswordHash.Should().Be(before.PasswordHash);
        (await GoogleKeysAsync(before.Id)).Should().BeEmpty();

        await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(token, fullName: "Ana Google"), HttpStatusCode.Created);

        var after = (await UserAsync(email))!;
        after.Id.Should().Be(before.Id, "the pending account is taken over, never duplicated");
        after.Status.Should().Be(AccountStatus.Active);
        after.PasswordHash.Should().BeNull();
        after.SecurityStamp.Should().NotBe(before.SecurityStamp);
        after.FullName.Should().Be("Ana Google");
        after.IsAdultDeclared.Should().BeTrue();
        (await GoogleKeysAsync(after.Id)).Should().Equal(subject);
        (await QueryAsync(context => context.ConsentRecords.CountAsync(record => record.UserId == after.Id))).Should().Be(2);
        (await QueryAsync(context => context.Users.CountAsync(user => user.Email == email))).Should().Be(1);
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error
            .Should().Be(IdentityErrorCodes.InvalidCredentials, "whoever set that password never proved the address");
        (await GoogleTokens.GrantAsync(client, token)).AccessToken.Should().NotBeNull();
    }

    // AC19.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Grant_AddressGoogleDoesNotVouchFor_DoesNotReachTheAccount(bool active)
    {
        var client = Client();
        var email = $"ana.{Guid.CreateVersion7():N}@exemplo.com";
        if (active)
        {
            await ActiveUser.CreateAsync(client, Factory, email);
        }
        else
        {
            await PostAsync(client, "/api/v1/identity/registrations", SignUpForm.Valid(email), HttpStatusCode.Accepted);
        }

        var before = (await UserAsync(email))!;
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), email);

        var grant = await GoogleTokens.GrantAsync(client, token);
        var registration = await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(token), HttpStatusCode.Conflict);

        grant.Error.Should().Be(IdentityErrorCodes.GoogleAccountExists);
        grant.AccessToken.Should().BeNull();
        CodeOf(registration).Should().Be(IdentityErrorCodes.GoogleAccountExists);
        var after = (await UserAsync(email))!;
        after.Status.Should().Be(before.Status);
        after.PasswordHash.Should().Be(before.PasswordHash);
        (await GoogleKeysAsync(after.Id)).Should().BeEmpty();
    }

    // F-29 AC16 (was F-20 AC19): a Workspace address is vouched for just the same, and is refused just the
    // same. Being authoritative decides the pending takeover, never a link into an active account any more.
    [Fact]
    public async Task Grant_WorkspaceAccountWithTheSameAddress_IsRefusedAndLinksNothing()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, $"ana.{Guid.CreateVersion7():N}@exemplo.com");
        var subject = GoogleTokens.NewSubject();

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email, hostedDomain: "exemplo.com"));

        grant.AccessToken.Should().BeNull();
        grant.Error.Should().Be(IdentityErrorCodes.GoogleAccountExists);
        (await GoogleKeysAsync((await UserAsync(email))!.Id)).Should().BeEmpty();
    }

    // AC11.
    [Fact]
    public async Task Grant_AddressLinkedToAnotherGoogleSubject_DoesNotSignIn()
    {
        var client = Client();
        var email = await RegisterAsync(client, GoogleTokens.NewSubject(), NewEmail());

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(GoogleTokens.NewSubject(), email));

        grant.AccessToken.Should().BeNull();
        grant.Error.Should().Be(IdentityErrorCodes.GoogleAccountExists);
    }

    // AC12.
    [Fact]
    public async Task Register_ActiveAccountCreatedBeforeTheConfirmation_AnswersAccountExistsAndCreatesNoSecondOne()
    {
        var client = Client();
        var email = NewEmail();
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), email);
        (await GoogleTokens.GrantAsync(client, token)).Error.Should().Be(IdentityErrorCodes.GoogleSignUpRequired);
        await ActiveUser.CreateAsync(client, Factory, email);

        var body = await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(token), HttpStatusCode.Conflict);

        CodeOf(body).Should().Be(IdentityErrorCodes.GoogleAccountExists);
        (await QueryAsync(context => context.Users.CountAsync(user => user.Email == email))).Should().Be(1);
    }

    // AC13.
    [Fact]
    public async Task Register_EleventhRegistrationFromOneClientInAnHour_IsRateLimited()
    {
        var client = Client();
        for (var index = 0; index < 10; index++)
        {
            await PostAsync(client, "/api/v1/identity/registrations", SignUpForm.Valid(), HttpStatusCode.Accepted);
        }

        var email = NewEmail();
        var body = await PostAsync(client, RegistrationRoute, GoogleTokens.Registration(GoogleTokens.Issue(GoogleTokens.NewSubject(), email)), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.RegistrationRateLimited);
        (await UserAsync(email)).Should().BeNull();
    }

    // AC16.
    [Theory]
    [InlineData("/api/v1/identity/account-erasures")]
    [InlineData("/api/v1/identity/data-exports")]
    [InlineData("/api/v1/identity/password-changes")]
    public async Task PasswordAction_AccountWithoutPassword_AnswersPasswordNotSet(string route)
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = await RegisterAsync(client, subject, NewEmail());
        var session = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));
        object body = route.EndsWith("password-changes", StringComparison.Ordinal)
            ? new ChangePasswordRequest("anything", "Novo#Segredo2026")
            : new DataExportRequest("anything");

        using var response = await TotpApi.SendAsync(client, HttpMethod.Post, route, session.AccessToken!, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.PasswordNotSet);
        (await UserAsync(email)).Should().NotBeNull("nothing was erased");
    }

    // AC16: "forgot password" gives the first password, and the action then works with it.
    [Fact]
    public async Task PasswordAction_AfterAResetSetsAFirstPassword_Works()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = await RegisterAsync(client, subject, NewEmail());
        await SetPasswordThroughResetAsync(client, email, SignUpForm.ValidPassword);
        var session = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        using var response = await TotpApi.SendAsync(
            client, HttpMethod.Post, "/api/v1/identity/data-exports", session.AccessToken!, new DataExportRequest(SignUpForm.ValidPassword));

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    // AC17.
    [Fact]
    public async Task Grant_AfterTheLinkedAccountWasErased_AsksForSignUp()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = await RegisterAsync(client, subject, NewEmail());
        var userId = (await UserAsync(email))!.Id;
        await SetPasswordThroughResetAsync(client, email, SignUpForm.ValidPassword);
        var session = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));
        using (var erased = await TotpApi.SendAsync(
            client, HttpMethod.Post, "/api/v1/identity/account-erasures", session.AccessToken!, new EraseAccountRequest(SignUpForm.ValidPassword)))
        {
            erased.IsSuccessStatusCode.Should().BeTrue(await erased.Content.ReadAsStringAsync());
        }

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        (await GoogleKeysAsync(userId)).Should().BeEmpty();
        grant.Error.Should().Be(IdentityErrorCodes.GoogleSignUpRequired);
    }

    private async Task SetPasswordThroughResetAsync(HttpClient client, string email, string password)
    {
        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(client, "/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        var token = ResetLink.TokenOf(Emails.Last!.HtmlBody);
        using var reset = await client.PostAsJsonAsync("/api/v1/identity/password-resets", new ResetPasswordRequest(token, password), AppJson.Options);
        reset.IsSuccessStatusCode.Should().BeTrue(await reset.Content.ReadAsStringAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        (await users.HasPasswordAsync((await users.FindByEmailAsync(email))!)).Should().BeTrue();
    }
}
