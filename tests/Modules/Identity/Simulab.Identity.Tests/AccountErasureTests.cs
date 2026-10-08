using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.EntityFrameworkCore.Models;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Jobs;
using Simulab.Persistence;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-10: a user erases their own account (AC1-AC8, AC10, AC11).</summary>
public sealed class AccountErasureTests : IdentityApiTests
{
    private const string EraseRoute = "/api/v1/identity/account-erasures";

    private ErasedAccounts Erased { get; } = new();

    /// <summary>A second module would register its consumer the same way (BR13).</summary>
    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(Erased);
            services.AddIntegrationEventConsumer<UserErased, RecordingUserErasedConsumer>();
        });

    private static async Task<HttpResponseMessage> EraseAsync(HttpClient client, TokenResponse session, string? password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EraseRoute)
        {
            Content = JsonContent.Create(new EraseAccountRequest(password), options: AppJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.SendAsync(request);
    }

    /// <summary>The row as it is on disk, which every ordinary query hides once it is erased.</summary>
    private Task<User> ErasedRowAsync(Guid userId) =>
        QueryAsync(context => context.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == userId));

    // AC1, AC2, AC11.
    [Fact]
    public async Task Erase_WithTheRightPassword_AnonymizesTheRowAndPublishesTheEvent()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);

        using var response = await EraseAsync(client, session, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        var row = await ErasedRowAsync(userId);
        row.Email.Should().Be(User.TombstoneAddressFor(userId));
        row.NormalizedEmail.Should().Be(User.TombstoneAddressFor(userId).ToUpperInvariant());
        row.UserName.Should().Be(row.Email);
        row.FullName.Should().BeNull();
        row.PasswordHash.Should().BeNull();
        row.EmailConfirmed.Should().BeFalse();
        row.EmailVerifiedAt.Should().BeNull();
        row.IsAdultDeclared.Should().BeFalse();
        row.Status.Should().Be(AccountStatus.Erased);
        row.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().NotBeNull();

        // AC2: the old address and password open nothing.
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);

        // AC11 (F-59 BR6): published once by the account.erased job, with the erased account and the instant.
        Erased.Events.Should().BeEmpty();
        await RunJobsAsync();
        Erased.Events.Should().ContainSingle();
        Erased.Events[0].UserId.Should().Be(userId);
        Erased.Events[0].ErasedAt.Should().Be(Factory.Clock.GetUtcNow());
    }

    // AC3.
    [Fact]
    public async Task Erase_ThenPasswordReset_ForTheOldAddress_SendsNothing()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        using (await EraseAsync(client, session, SignUpForm.ValidPassword))
        {
        }

        await RunJobsAsync();
        Emails.Clear();
        using var reset = await client.PostAsJsonAsync(
            "/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), AppJson.Options);

        // The same answer an unknown address gets (F-7 BR12), and no email at all.
        reset.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    // AC4, AC8.
    [Fact]
    public async Task Erase_DropsTheAccountsOwnRowsAndLeavesTheConsentWithoutTheAddress()
    {
        var client = Client();
        var form = SignUpForm.Valid();
        using (var signUp = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(form, options: AppJson.Options)
        })
        {
            signUp.Headers.Add(ClientAddressHeaders.Address, "203.0.113.10");
            signUp.Headers.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
            using var accepted = await client.SendAsync(signUp);
            accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        await RunJobsAsync();
        var raw = VerificationLink.TokenOf(Emails.Last!.HtmlBody);
        await client.PostAsJsonAsync("/api/v1/identity/email-verifications", new VerifyEmailRequest(raw), AppJson.Options);

        var userId = await UserIdOfAsync(form.Email);
        var session = (await SignedInSessions.CreateAsync(client, form.Email, SignUpForm.ValidPassword, 1))[0];

        // The account holds the Student role (F-4 BR2) and has a used verification token.
        (await QueryAsync(context => context.UserRoles.CountAsync(role => role.UserId == userId))).Should().Be(1);
        (await QueryAsync(context => context.EmailVerificationTokens.IgnoreQueryFilters().CountAsync(token => token.UserId == userId))).Should().BePositive();

        using var response = await EraseAsync(client, session, SignUpForm.ValidPassword);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await QueryAsync(context => context.UserRoles.CountAsync(role => role.UserId == userId))).Should().Be(0);
        (await QueryAsync(context => context.EmailVerificationTokens.IgnoreQueryFilters().CountAsync(token => token.UserId == userId))).Should().Be(0);
        (await QueryAsync(context => context.PasswordResetTokens.IgnoreQueryFilters().CountAsync(token => token.UserId == userId))).Should().Be(0);
        (await QueryAsync(context => context.UserClaims.CountAsync(claim => claim.UserId == userId))).Should().Be(0);
        (await QueryAsync(context => context.UserTokens.CountAsync(token => token.UserId == userId))).Should().Be(0);

        var subject = userId.ToString();
        (await QueryAsync(context => context.Set<OpenIddictEntityFrameworkCoreToken>().CountAsync(token => token.Subject == subject))).Should().Be(0);
        (await QueryAsync(context => context.Set<OpenIddictEntityFrameworkCoreAuthorization>().CountAsync(grant => grant.Subject == subject))).Should().Be(0);

        // BR9: the evidence stays, the address goes.
        var consent = await QueryAsync(context => context.ConsentRecords
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(record => record.UserId == userId));
        consent.IpAddress.Should().BeNull();
        consent.TermsVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.PrivacyVersion.Should().Be(SignUpForm.CurrentVersion);
        consent.DeclaresAdult.Should().BeTrue();
    }

    // AC4: the back office no longer sees the account.
    [Fact]
    public async Task Erase_RemovesTheAccountFromTheBackOfficeList()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await UserIdOfAsync(email);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var adminClient = await Accounts.SignedInAsync(Client(), admin.Email!);
        var before = await adminClient.GetFromJsonAsync<UserPageResponse>("/api/v1/identity/users?pageSize=100", AppJson.Options);
        before!.Items.Should().Contain(user => user.Id == userId);

        using var response = await EraseAsync(client, session, SignUpForm.ValidPassword);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await adminClient.GetFromJsonAsync<UserPageResponse>("/api/v1/identity/users?pageSize=100", AppJson.Options);
        after!.Items.Should().NotContain(user => user.Id == userId);
    }

    // AC5.
    [Fact]
    public async Task Erase_FreesTheAddressForANewAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var firstId = await UserIdOfAsync(email);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        using (await EraseAsync(client, session, SignUpForm.ValidPassword))
        {
        }

        var again = await ActiveUser.CreateAsync(client, Factory, email);

        again.Should().Be(email);
        var secondId = await UserIdOfAsync(email);
        secondId.Should().NotBe(firstId);
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
    }

    // AC6.
    [Fact]
    public async Task Erase_WrongCurrentPassword_ChangesNothingAndCountsTowardTheLockout()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);

        for (var attempt = 1; attempt < 5; attempt++)
        {
            using var wrong = await EraseAsync(client, session, "not-the-password");
            wrong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            CodeOf(await wrong.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid);
        }

        var untouched = await ErasedRowAsync(userId);
        untouched.Email.Should().Be(email);
        untouched.IsDeleted.Should().BeFalse();
        untouched.AccessFailedCount.Should().Be(4);
        Erased.Events.Should().BeEmpty();

        using var fifth = await EraseAsync(client, session, "not-the-password");
        using var rightButLocked = await EraseAsync(client, session, SignUpForm.ValidPassword);

        fifth.StatusCode.Should().Be(HttpStatusCode.Locked);
        rightButLocked.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body = JsonDocument.Parse(await rightButLocked.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AccountLocked);
        body.GetProperty("retryAfterSeconds").GetInt32().Should().BePositive();
        (await ErasedRowAsync(userId)).IsDeleted.Should().BeFalse();
    }

    // AC6: no token, no erasure.
    [Fact]
    public async Task Erase_WithoutAToken_IsUnauthorized()
    {
        using var response = await Client().PostAsJsonAsync(EraseRoute, new EraseAccountRequest(SignUpForm.ValidPassword), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC7.
    [Fact]
    public async Task Erase_EndsEverySessionOfTheAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 3);

        using var response = await EraseAsync(client, sessions[0], SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        foreach (var session in sessions)
        {
            (await SignedInSessions.AnswerOfAsync(client, session.AccessToken!)).Should().Be((HttpStatusCode.Unauthorized, IdentityErrorCodes.TokenRevoked));

            // `invalid_grant`, not our own code: BR8 deleted the stored OpenIddict token, so the protocol
            // rejects the refresh token before the module's own session check ever runs.
            var refreshed = await TokenClient.RefreshAsync(client, session.RefreshToken!);
            refreshed.AccessToken.Should().BeNull();
            refreshed.Error.Should().Be("invalid_grant");
        }
    }

    // AC10: the account's own language, and a mail failure that does not undo the erasure.
    [Fact]
    public async Task Erase_SendsTheFarewellEmailInTheAccountsLanguage()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        using var language = new HttpRequestMessage(HttpMethod.Put, "/api/v1/identity/profile/preferred-language")
        {
            Content = JsonContent.Create(new UpdatePreferredLanguageRequest("pt-PT"), options: AppJson.Options)
        };
        language.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        (await client.SendAsync(language)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await RunJobsAsync();
        Emails.Clear();
        using var response = await EraseAsync(client, session, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        // It goes to the real address, before the tombstone replaced it.
        Emails.Last!.To.Should().Be(email);
        Emails.Last.Subject.Should().Be("A sua conta foi eliminada — Simulab");
        Emails.Last.HtmlBody.Should().Contain("https://localhost/sign-up");
    }

    // AC10: a mail server that is down does not undo an erasure that already happened.
    [Fact]
    public async Task Erase_WhenTheFarewellEmailFails_StillErasesTheAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);

        await RunJobsAsync();
        Emails.Clear();
        Emails.FailNext = true;
        using var response = await EraseAsync(client, session, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ErasedRowAsync(userId)).Status.Should().Be(AccountStatus.Erased);
        Erased.Events.Should().BeEmpty();

        // F-13: the farewell email is a job now. The worker's failure does not touch the erasure, and
        // the message is tried again instead of being lost.
        await RunJobsAsync();
        Emails.Count.Should().Be(0);
        Erased.Events.Should().ContainSingle(); // the event job is a job of its own: the failed email does not hold it
        (await PendingJobCountAsync()).Should().Be(1);

        Factory.Clock.Advance(JobPolicy.BackoffAfter(1));
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        Emails.Last!.To.Should().Be(email);
    }

    private Task<Guid> UserIdOfAsync(string email) =>
        QueryAsync(context => context.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync());
}
