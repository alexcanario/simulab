using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-21 through HTTP: what happens to an account leaves an event, and the trail lists them. The tests of this
/// class share one database, so each one reads the trail filtered by an account of its own.
/// </summary>
public sealed class AccountEventTrailTests : IdentityApiTests
{
    private const string AccountEvents = "/api/v1/identity/account-events";
    private const string Visitor = "203.0.113.10";

    // AC1: the password is the last step, so the sign-in is recorded with the way it was passed and the address.
    [Fact]
    public async Task SignIn_WithThePassword_RecordsTheAccountTheTimeTheWayAndTheAddress()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await IdOfAsync(email);

        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        token.AccessToken.Should().NotBeNull(token.ErrorDescription);
        var entry = await SingleAsync(userId);
        entry.Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        entry.Method.Should().Be(nameof(AccountEventMethod.Password));
        entry.Reason.Should().BeNull();
        entry.IpAddress.Should().Be(Visitor);
        entry.OccurredAt.Should().Be(Factory.Clock.GetUtcNow());
        entry.Account.Should().Be(new AccountEventAccountResponse(userId, email));
    }

    // AC2: a silent renewal is not a sign-in.
    [Fact]
    public async Task RefreshGrant_RecordsNothing()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await IdOfAsync(email);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var refreshed = await TokenClient.RefreshAsync(client, token.RefreshToken!);

        refreshed.AccessToken.Should().NotBeNull(refreshed.ErrorDescription);
        (await EventsAsync(userId)).Should().ContainSingle().Which.Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
    }

    // AC3: a wrong password and an unverified account, each with its reason.
    [Fact]
    public async Task SignIn_WrongPasswordAndUnverifiedAccount_RecordTheirReasons()
    {
        var client = Visiting();
        var active = await ActiveUser.CreateAsync(client, Factory);
        var pending = await Accounts.CreateAsync(Factory.Services, active: false);

        await TokenClient.SignInAsync(client, active, "not-the-password");
        await TokenClient.SignInAsync(client, pending.Email!, SignUpForm.ValidPassword);

        var wrongPassword = await SingleAsync(await IdOfAsync(active));
        wrongPassword.Event.Should().Be(nameof(AccountEventType.SignInFailed));
        wrongPassword.Reason.Should().Be(nameof(AccountEventReason.WrongPassword));
        wrongPassword.IpAddress.Should().Be(Visitor);

        var notVerified = await SingleAsync(pending.Id);
        notVerified.Event.Should().Be(nameof(AccountEventType.SignInFailed));
        notVerified.Reason.Should().Be(nameof(AccountEventReason.EmailNotVerified));
    }

    // AC4: the attempt is recorded, the typed name is not.
    [Fact]
    public async Task SignIn_WithAUserNameThatMatchesNoAccount_RecordsTheAttemptWithoutTheName()
    {
        var client = Visiting();
        const string Typed = "nobody.at.all@exemplo.com";

        await TokenClient.SignInAsync(client, Typed, SignUpForm.ValidPassword);

        var entry = (await ListAsync($"?event={nameof(AccountEventType.SignInFailed)}&ip={Visitor}")).Items
            .Should().ContainSingle(item => item.Account == null).Subject;
        entry.Reason.Should().Be(nameof(AccountEventReason.UnknownAccount));
        entry.IpAddress.Should().Be(Visitor);

        var stored = await QueryAsync(context => context.AccountEvents.AsNoTracking().ToListAsync());
        stored.Should().NotContain(accountEvent => accountEvent.UserId == null && accountEvent.IpAddress != Visitor);
        string.Join(" ", stored.Select(accountEvent => $"{accountEvent.Type}{accountEvent.Reason}{accountEvent.IpAddress}"))
            .Should().NotContain(Typed);
    }

    // AC5: the failure that crosses the limit is one lockout; the attempts that follow are failures again.
    [Fact]
    public async Task SignIn_TheFailureThatCrossesTheLimit_RecordsOneLockoutAndTheNextAttemptIsAFailure()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await IdOfAsync(email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "not-the-password");
        }

        var afterFive = await EventsAsync(userId);
        afterFive.Count(entry => entry.Event == nameof(AccountEventType.AccountLocked)).Should().Be(1);
        afterFive.Count(entry => entry.Event == nameof(AccountEventType.SignInFailed)).Should().Be(5);

        await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var afterSix = await EventsAsync(userId);
        afterSix.Count(entry => entry.Event == nameof(AccountEventType.AccountLocked)).Should().Be(1);
        afterSix.Should().ContainSingle(entry =>
            entry.Event == nameof(AccountEventType.SignInFailed) && entry.Reason == nameof(AccountEventReason.LockedOut));
    }

    // AC6: sign-out, password changed, and a reset asked for and completed.
    [Fact]
    public async Task SignOutPasswordChangeAndReset_EachRecordTheirOwnEvent()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await IdOfAsync(email);
        var signedIn = await Accounts.SignedInAsync(Visiting(), email);

        const string NewPassword = "Outra-Senha-9876";
        var changed = await signedIn.PostAsJsonAsync(
            "/api/v1/identity/password-changes",
            new ChangePasswordRequest(SignUpForm.ValidPassword, NewPassword),
            AppJson.Options);
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent, await changed.Content.ReadAsStringAsync());

        var signedOut = await signedIn.PostAsync("/api/v1/identity/sign-out", content: null);
        signedOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await client.PostAsJsonAsync("/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), AppJson.Options);
        await RunJobsAsync();
        var link = ResetLink.TokenOf(Emails.Last!.HtmlBody);
        var reset = await client.PostAsJsonAsync(
            "/api/v1/identity/password-resets",
            new ResetPasswordRequest(link, "Terceira-Senha-4321"),
            AppJson.Options);
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync());

        (await EventsAsync(userId)).Select(entry => entry.Event).Should().BeEquivalentTo(
        [
            nameof(AccountEventType.SignInSucceeded),
            nameof(AccountEventType.PasswordChanged),
            nameof(AccountEventType.SignedOut),
            nameof(AccountEventType.PasswordResetRequested),
            nameof(AccountEventType.PasswordResetCompleted)
        ]);
    }

    // AC6: an address that belongs to no account leaves nothing behind (BR6).
    [Fact]
    public async Task PasswordResetRequested_ForAnAddressWithNoAccount_RecordsNothing()
    {
        var client = Visiting();
        var before = await CountAsync();

        var asked = await client.PostAsJsonAsync(
            "/api/v1/identity/password-reset-requests",
            new RequestPasswordResetRequest("ninguem.aqui@exemplo.com"),
            AppJson.Options);

        asked.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await CountAsync()).Should().Be(before);
    }

    // AC7: a refused change records nothing of that change.
    [Fact]
    public async Task RefusedPasswordChange_RecordsNoPasswordEvent()
    {
        var email = await ActiveUser.CreateAsync(Visiting(), Factory);
        var userId = await IdOfAsync(email);
        var signedIn = await Accounts.SignedInAsync(Visiting(), email);

        var refused = await signedIn.PostAsJsonAsync(
            "/api/v1/identity/password-changes",
            new ChangePasswordRequest("not-the-password", "Outra-Senha-9876"),
            AppJson.Options);

        refused.IsSuccessStatusCode.Should().BeFalse();
        (await EventsAsync(userId)).Should().NotContain(entry => entry.Event == nameof(AccountEventType.PasswordChanged));
    }

    // AC9: the erasure is recorded and the account's events lose their address; other accounts keep theirs.
    [Fact]
    public async Task AccountErasure_RecordsItselfAndClearsTheAddressOfThatAccountsEventsOnly()
    {
        var erasedEmail = await ActiveUser.CreateAsync(Visiting(), Factory);
        var erasedId = await IdOfAsync(erasedEmail);
        var otherEmail = await ActiveUser.CreateAsync(Visiting(), Factory);
        var otherId = await IdOfAsync(otherEmail);
        var signedIn = await Accounts.SignedInAsync(Visiting(), erasedEmail);

        var erased = await signedIn.PostAsJsonAsync(
            "/api/v1/identity/account-erasures",
            new EraseAccountRequest(SignUpForm.ValidPassword),
            AppJson.Options);

        erased.StatusCode.Should().Be(HttpStatusCode.NoContent, await erased.Content.ReadAsStringAsync());
        var events = await EventsAsync(erasedId);
        events.Select(entry => entry.Event).Should().Contain(nameof(AccountEventType.AccountErased));
        events.Should().OnlyContain(entry => entry.IpAddress == null);
        events.Should().OnlyContain(entry => entry.Account!.Email == null);
        (await EventsAsync(otherId)).Should().OnlyContain(entry => entry.IpAddress == Visitor);
    }

    // AC8: the filters combine, the page is newest first, and a period or an event outside the set is refused.
    [Fact]
    public async Task List_FiltersCombineNewestFirst_AndRefusesAnUnknownPeriodOrEvent()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var userId = await IdOfAsync(email);
        await TokenClient.SignInAsync(client, email, "not-the-password");
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var all = await ListAsync($"?user={userId}");
        all.Total.Should().Be(2);
        all.Items[0].Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        all.Items[1].Event.Should().Be(nameof(AccountEventType.SignInFailed));
        all.Account.Should().Be(new AccountEventAccountResponse(userId, email));

        (await ListAsync($"?user={userId}&event={nameof(AccountEventType.SignInFailed)}")).Items
            .Should().ContainSingle().Which.Reason.Should().Be(nameof(AccountEventReason.WrongPassword));
        (await ListAsync($"?user={userId}&ip=198.51.100.7")).Items.Should().BeEmpty();
        (await ListAsync($"?user={userId}&search={email}")).Total.Should().Be(2);
        (await ListAsync($"?user={userId}&days=90")).Total.Should().Be(2);
        (await ListAsync($"?user={userId}&pageSize=1")).Items.Should().ContainSingle();

        await RefusedAsync($"?days=5", IdentityErrorCodes.AccountEventPeriodInvalid);
        await RefusedAsync("?event=NotAnEvent", IdentityErrorCodes.AccountEventTypeInvalid);
    }

    // AC10: nothing edits or deletes an event, and an event holds no email, name or user agent.
    [Fact]
    public async Task Trail_HasNoWriteRouteAndHoldsNoPersonalData()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var (admin, _) = await AdminAsync();

        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
        {
            using var request = new HttpRequestMessage(method, AccountEvents);
            using var response = await admin.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed, $"{method} must not exist");
        }

        var stored = await QueryAsync(context => context.AccountEvents.AsNoTracking().ToListAsync());
        var text = string.Join(" ", stored.Select(entry => $"{entry.Type} {entry.Method} {entry.Reason} {entry.IpAddress}"));
        text.Should().NotContain(email).And.NotContain("Ana");
    }

    private HttpClient Visiting()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Address, Visitor);
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
        return client;
    }

    private async Task<(HttpClient Client, Guid Id)> AdminAsync()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        return (await Accounts.SignedInAsync(Visiting(), admin.Email!), admin.Id);
    }

    private Task<Guid> IdOfAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().Where(user => user.Email == email).Select(user => user.Id).SingleAsync());

    private Task<int> CountAsync() => QueryAsync(context => context.AccountEvents.CountAsync());

    private async Task<IReadOnlyList<AccountEventResponse>> EventsAsync(Guid userId) =>
        (await ListAsync($"?user={userId}&pageSize=100")).Items;

    private async Task<AccountEventResponse> SingleAsync(Guid userId) =>
        (await EventsAsync(userId)).Should().ContainSingle().Subject;

    /// <summary>The trail is read with <c>identity.roles.manage</c> (BR11), so every read goes through an Admin.</summary>
    private async Task<AccountEventPageResponse> ListAsync(string query)
    {
        var (admin, _) = await AdminAsync();
        var response = await admin.GetAsync(AccountEvents + query);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AccountEventPageResponse>(AppJson.Options))!;
    }

    private async Task RefusedAsync(string query, string code)
    {
        var (admin, _) = await AdminAsync();
        var response = await admin.GetAsync(AccountEvents + query);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
    }
}
