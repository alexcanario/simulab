using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-21 with Google sign-in on: Google as the way in (BR3), and a token its checks refused (BR4).</summary>
public sealed class AccountEventGoogleTests : IdentityApiTests
{
    private const string AccountEvents = "/api/v1/identity/account-events";

    protected override void ConfigureHost(IWebHostBuilder builder) => GoogleTokens.Configure(builder);

    // AC1: Google was the last step.
    [Fact]
    public async Task SignIn_WithGoogle_RecordsThatMethod()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = $"ana.{Guid.CreateVersion7():N}@gmail.com";
        await PostAsync(client, "/api/v1/identity/google-registrations", GoogleTokens.Registration(GoogleTokens.Issue(subject, email)), HttpStatusCode.Created);
        var userId = await IdOfAsync(email);

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        grant.AccessToken.Should().NotBeNull(grant.Error);
        var signIn = (await EventsAsync(userId)).Should().ContainSingle().Subject;
        signIn.Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        signIn.Method.Should().Be(nameof(AccountEventMethod.Google));
    }

    // AC3, AC4: a refused token names no account, and nothing of it is stored.
    [Fact]
    public async Task Grant_WithARefusedToken_RecordsAFailedSignInWithNoAccount()
    {
        var client = Client();
        var email = $"ana.{Guid.CreateVersion7():N}@gmail.com";
        var broken = GoogleTokens.Issue(GoogleTokens.NewSubject(), email, issuer: "https://accounts.example.com");

        var grant = await GoogleTokens.GrantAsync(client, broken);

        grant.Error.Should().Be(IdentityErrorCodes.GoogleTokenInvalid);
        var entry = (await ListAsync($"?event={nameof(AccountEventType.SignInFailed)}")).Items
            .Should().ContainSingle().Subject;
        entry.Account.Should().BeNull();
        entry.Reason.Should().Be(nameof(AccountEventReason.GoogleTokenRefused));

        var stored = await QueryAsync(context => context.AccountEvents.AsNoTracking().ToListAsync());
        string.Join(" ", stored.Select(item => $"{item.Type}{item.Reason}")).Should().NotContain(email);
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
