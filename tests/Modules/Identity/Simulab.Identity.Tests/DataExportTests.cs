using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-16: a user downloads their own data (AC1-AC8, AC10).</summary>
public sealed class DataExportTests : IdentityApiTests
{
    private const string ExportRoute = "/api/v1/identity/data-exports";
    private const string ClientIp = "203.0.113.20";

    /// <summary>TOTP on, so AC3 can prove the secret stays out of the file.</summary>
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
    }

    private static async Task<HttpResponseMessage> ExportAsync(HttpClient client, string? accessToken, string? password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ExportRoute)
        {
            Content = JsonContent.Create(new DataExportRequest(password), options: AppJson.Options)
        };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    /// <summary>An active account signed up from <see cref="ClientIp"/>, so its consent record carries an address.</summary>
    private async Task<string> SignedUpFromAnAddressAsync(HttpClient client)
    {
        var form = SignUpForm.Valid();
        using (var signUp = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(form, options: AppJson.Options)
        })
        {
            signUp.Headers.Add(ClientAddressHeaders.Address, ClientIp);
            signUp.Headers.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
            using var accepted = await client.SendAsync(signUp);
            accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        await RunJobsAsync();
        var raw = VerificationLink.TokenOf(Emails.Last!.HtmlBody);
        await client.PostAsJsonAsync("/api/v1/identity/email-verifications", new VerifyEmailRequest(raw), AppJson.Options);
        return form.Email;
    }

    /// <summary>An administrator gives <paramref name="userId"/> the Admin role on top of Student: one role change targets the user.</summary>
    private async Task<(Guid AdminId, string AdminEmail)> AdminAddsARoleAsync(Guid userId)
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var adminClient = await Accounts.SignedInAsync(Client(), admin.Email!);
        var roles = (await adminClient.GetFromJsonAsync<List<RoleResponse>>("/api/v1/identity/roles", AppJson.Options))!
            .ToDictionary(role => role.Name, role => role.Id);
        using var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/identity/users/{userId}/roles",
            new SetUserRolesRequest([roles[IdentityRoles.Student], roles[IdentityRoles.Admin]]),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (admin.Id, admin.Email!);
    }

    private Task<Guid> UserIdOfAsync(string email) =>
        QueryAsync(context => context.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync());

    private Task<int> AccessFailedCountAsync(Guid userId) =>
        QueryAsync(context => context.Users.Where(user => user.Id == userId).Select(user => user.AccessFailedCount).SingleAsync());

    // AC1, AC2, AC8.
    [Fact]
    public async Task Export_WithTheRightPassword_ReturnsTheAccountsDataAsAJsonAttachment()
    {
        var client = Client();
        var email = await SignedUpFromAnAddressAsync(client);
        var userId = await UserIdOfAsync(email);
        var (adminId, adminEmail) = await AdminAddsARoleAsync(userId);
        var other = await ActiveUser.CreateAsync(Client(), Factory);
        var sessions = await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 2);

        using var response = await ExportAsync(client, sessions[0].AccessToken, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var date = Factory.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        response.Content.Headers.ContentDisposition!.FileName!.Trim('"').Should().Be($"simulab-my-data-{date}.json");

        var text = await response.Content.ReadAsStringAsync();
        var file = JsonSerializer.Deserialize<DataExportResponse>(text, AppJson.Options)!;
        file.Format.Should().Be("simulab.data-export");
        file.Version.Should().Be(1);
        file.ExportedAt.Should().Be(Factory.Clock.GetUtcNow());

        var identity = file.Identity;
        identity.Account.Id.Should().Be(userId);
        identity.Account.Email.Should().Be(email);
        identity.Account.PreferredLanguage.Should().Be("en");
        identity.Account.Status.Should().Be("Active");
        identity.Account.EmailVerifiedAt.Should().NotBeNull();
        identity.Roles.Should().BeEquivalentTo(IdentityRoles.Admin, IdentityRoles.Student);
        identity.Consents.Should().ContainSingle().Which.IpAddress.Should().Be(ClientIp);
        var change = identity.RoleChanges.Should().ContainSingle().Subject;
        change.Added.Should().Equal(IdentityRoles.Admin);
        change.Removed.Should().BeEmpty();
        change.ByYou.Should().BeFalse();
        identity.Sessions.Active.Should().Be(2);

        // AC2: nothing of another user, and the administrator only as "not you".
        text.Should().NotContain(other).And.NotContain(adminEmail).And.NotContain(adminId.ToString());
        text.Should().NotContain((await UserIdOfAsync(other)).ToString());
    }

    // AC3.
    [Fact]
    public async Task Export_NeverContainsSecretsOrSecurityMaterial()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        using (var confirmed = await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Factory.Clock.GetUtcNow())))
        {
            confirmed.StatusCode.Should().Be(HttpStatusCode.OK, await confirmed.Content.ReadAsStringAsync());
        }

        var userId = await UserIdOfAsync(email);
        var row = await QueryAsync(context => context.Users.SingleAsync(user => user.Id == userId));
        row.TotpSecretEncrypted.Should().NotBeNull();
        var sessionJti = await SignedInSessions.SessionJtiAsync(client, session.AccessToken!);

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await response.Content.ReadAsStringAsync();

        text.Should().NotContain(row.PasswordHash!)
            .And.NotContain(row.SecurityStamp!)
            .And.NotContain(row.ConcurrencyStamp!)
            .And.NotContain(row.TotpSecretEncrypted!)
            .And.NotContain(enrolment.Secret)
            .And.NotContain(sessionJti)
            .And.NotContain(session.AccessToken!)
            .And.NotContain(session.RefreshToken!);
        foreach (var name in new[] { "passwordHash", "securityStamp", "concurrencyStamp", "totp", "accessFailed", "lockout", "token" })
        {
            text.Should().NotContainEquivalentOf(name);
        }
    }

    // AC4.
    [Fact]
    public async Task Export_WrongPassword_FailsCountsTheFailureAndSendsNothing()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);
        await RunJobsAsync();
        var jobsBefore = (await JobsAsync()).Count;

        using var response = await ExportAsync(client, session.AccessToken, "not-the-password");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.DataExportCurrentPasswordInvalid);
        (await AccessFailedCountAsync(userId)).Should().Be(1);
        (await JobsAsync()).Count.Should().Be(jobsBefore);
    }

    // AC5.
    [Fact]
    public async Task Export_WhileLockedOut_IsRefusedEvenWithTheRightPassword()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        for (var attempt = 1; attempt < 5; attempt++)
        {
            using var wrong = await ExportAsync(client, session.AccessToken, "not-the-password");
            wrong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        using var fifth = await ExportAsync(client, session.AccessToken, "not-the-password");
        using var rightButLocked = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        fifth.StatusCode.Should().Be(HttpStatusCode.Locked);
        rightButLocked.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body = JsonDocument.Parse(await rightButLocked.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AccountLocked);
        body.GetProperty("retryAfterSeconds").GetInt32().Should().BePositive();
    }

    // AC6.
    [Fact]
    public async Task Export_AfterAFailedAttempt_SucceedsAndClearsTheFailureCount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);

        using (var wrong = await ExportAsync(client, session.AccessToken, "not-the-password"))
        {
            wrong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        (await AccessFailedCountAsync(userId)).Should().Be(1);

        using var right = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        right.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AccessFailedCountAsync(userId)).Should().Be(0);
    }

    // AC7, AC8.
    [Fact]
    public async Task Export_QueuesOneNoticeInTheAccountsLanguageAndStoresNothingElse()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];

        using var language = new HttpRequestMessage(HttpMethod.Put, "/api/v1/identity/profile/preferred-language")
        {
            Content = JsonContent.Create(new UpdatePreferredLanguageRequest("pt-BR"), options: AppJson.Options)
        };
        language.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        (await client.SendAsync(language)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await RunJobsAsync();
        Emails.Clear();
        var jobsBefore = (await JobsAsync()).Count;

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await JobsAsync();
        jobs.Count.Should().Be(jobsBefore + 1);
        jobs.Should().ContainSingle(job => job.Type == "email.send" && job.Payload.Contains(email, StringComparison.Ordinal) && job.Payload.Contains("baixados", StringComparison.Ordinal));

        await RunJobsAsync();
        Emails.Count.Should().Be(1);
        Emails.Last!.To.Should().Be(email);
        Emails.Last.Subject.Should().Be("Seus dados foram baixados — Simulab");
        Emails.Last.TextBody.Should().Contain(" UTC");
    }

    // AC10.
    [Fact]
    public async Task Export_WithoutAToken_IsUnauthorized()
    {
        using var response = await ExportAsync(Client(), accessToken: null, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient Visiting()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Address, ClientIp);
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
        return client;
    }

    // F-32 AC1: a successful sign-in, a failed sign-in and a password change, oldest first with their own fields.
    [Fact]
    public async Task Export_AccountEvents_ListsThemOldestFirstWithTheirFields()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await UserIdOfAsync(email);
        var signedIn = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        signedIn.AccessToken.Should().NotBeNull(signedIn.ErrorDescription);
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await TokenClient.SignInAsync(client, email, "not-the-password");
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-changes")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(SignUpForm.ValidPassword, "Outra-Senha-9876"), options: AppJson.Options)
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signedIn.AccessToken);
        using var changed = await client.SendAsync(changeRequest);
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent, await changed.Content.ReadAsStringAsync());

        using var response = await ExportAsync(client, signedIn.AccessToken, "Outra-Senha-9876");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var file = JsonSerializer.Deserialize<DataExportResponse>(await response.Content.ReadAsStringAsync(), AppJson.Options)!;

        var events = file.Identity.AccountEvents;
        events.Should().HaveCount(3);
        events[0].Type.Should().Be(nameof(AccountEventType.SignInSucceeded));
        events[0].Method.Should().Be(nameof(AccountEventMethod.Password));
        events[0].Reason.Should().BeNull();
        events[0].IpAddress.Should().Be(ClientIp);
        events[1].Type.Should().Be(nameof(AccountEventType.SignInFailed));
        events[1].Method.Should().BeNull();
        events[1].Reason.Should().Be(nameof(AccountEventReason.WrongPassword));
        events[1].IpAddress.Should().Be(ClientIp);
        events[2].Type.Should().Be(nameof(AccountEventType.PasswordChanged));
        events[0].OccurredAt.Should().BeBefore(events[1].OccurredAt);
        events[1].OccurredAt.Should().BeBefore(events[2].OccurredAt);

        var stored = await QueryAsync(context => context.AccountEvents.AsNoTracking().Where(e => e.UserId == userId).ToListAsync());
        stored.Should().HaveCount(3);
    }

    // F-32 AC2: another account's events never appear.
    [Fact]
    public async Task Export_AccountEvents_NeverContainsAnotherUsersEvents()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var otherClient = Visiting();
        var otherEmail = await ActiveUser.CreateAsync(otherClient, Factory);
        await TokenClient.SignInAsync(otherClient, otherEmail, "not-the-password");

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        var file = JsonSerializer.Deserialize<DataExportResponse>(await response.Content.ReadAsStringAsync(), AppJson.Options)!;
        file.Identity.AccountEvents.Should().OnlyContain(e => e.Type == nameof(AccountEventType.SignInSucceeded));
    }

    // F-32 AC3: no cap and no pagination, past the admin trail's page-size cap (100).
    [Fact]
    public async Task Export_AccountEvents_HasNoCapEvenPastTheAdminTrailsPageSize()
    {
        const int EventCount = 105;
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await UserIdOfAsync(email);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        await QueryAsync(async context =>
        {
            context.AccountEvents.AddRange(Enumerable.Range(0, EventCount).Select(_ => AccountEvent.For(userId, AccountEventType.SignedOut, null)));
            await context.SaveChangesAsync();
            return 0;
        });

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        var file = JsonSerializer.Deserialize<DataExportResponse>(await response.Content.ReadAsStringAsync(), AppJson.Options)!;
        file.Identity.AccountEvents.Should().HaveCount(EventCount + 1);
    }

    // F-32 AC4: the export only reads the trail.
    [Fact]
    public async Task Export_AccountEvents_WritesNoNewEventAndChangesNone()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await UserIdOfAsync(email);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var before = await QueryAsync(context => context.AccountEvents.AsNoTracking().Where(e => e.UserId == userId).ToListAsync());

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var after = await QueryAsync(context => context.AccountEvents.AsNoTracking().Where(e => e.UserId == userId).ToListAsync());
        after.Should().BeEquivalentTo(before);
    }

    // F-32 AC6: an account with no recorded event exports an empty list, not missing and not null.
    [Fact]
    public async Task Export_AccountEvents_WithNoneRecorded_IsAnEmptyList()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = (await SignedInSessions.CreateAsync(client, email, SignUpForm.ValidPassword, 1))[0];
        var userId = await UserIdOfAsync(email);
        await QueryAsync(async context =>
        {
            await context.AccountEvents.Where(e => e.UserId == userId).ExecuteDeleteAsync();
            return 0;
        });

        using var response = await ExportAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        var text = await response.Content.ReadAsStringAsync();
        var file = JsonSerializer.Deserialize<DataExportResponse>(text, AppJson.Options)!;
        file.Identity.AccountEvents.Should().NotBeNull().And.BeEmpty();
        text.Should().Contain("\"accountEvents\":[]");
    }
}
