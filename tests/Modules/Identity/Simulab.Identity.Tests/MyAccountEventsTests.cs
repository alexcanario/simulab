using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-31, AC1-AC3, AC6: the signed-in caller's own account events, through the real HTTP pipeline.</summary>
public sealed class MyAccountEventsTests : IdentityApiTests
{
    private const string MyAccountEvents = "/api/v1/identity/account-events/mine";
    private const string Visitor = "203.0.113.10";

    private HttpClient Visiting()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Address, Visitor);
        client.DefaultRequestHeaders.Add(ClientAddressHeaders.Secret, TestClient.ClientSecret);
        return client;
    }

    // AC1, AC3: the caller's own events only, including their own failed sign-ins, newest first.
    [Fact]
    public async Task Get_SignedIn_ReturnsOnlyTheCallersOwnEventsNewestFirst()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await ActiveUser.CreateAsync(Visiting(), Factory);

        await TokenClient.SignInAsync(client, email, "not-the-password");
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var response = await GetAsync(token.AccessToken!);

        response.Total.Should().Be(2);
        response.Items[0].Event.Should().Be(nameof(AccountEventType.SignInSucceeded));
        response.Items[0].Method.Should().Be(nameof(AccountEventMethod.Password));
        response.Items[0].IpAddress.Should().Be(Visitor);
        response.Items[1].Event.Should().Be(nameof(AccountEventType.SignInFailed));
        response.Items.Should().OnlyContain(item => item.Account!.Email == email);
    }

    // AC3: the endpoint accepts no filters, so a caller asking for another account still gets only their own.
    [Fact]
    public async Task Get_IgnoresAnyQueryString_AndStillReturnsOnlyTheCallersOwnAccount()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var otherEmail = await ActiveUser.CreateAsync(Visiting(), Factory);
        var otherId = await QueryAsync(context => context.Users.AsNoTracking().Where(user => user.Email == otherEmail).Select(user => user.Id).SingleAsync());
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{MyAccountEvents}?user={otherId}&pageSize=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var httpResponse = await client.SendAsync(request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK, await httpResponse.Content.ReadAsStringAsync());
        var response = await httpResponse.Content.ReadFromJsonAsync<AccountEventPageResponse>(AppJson.Options);
        response!.Items.Should().NotBeEmpty();
        response.Items.Should().OnlyContain(item => item.Account!.Email == email);
    }

    // BR2: capped at 20, newest first, even when the account has more than that.
    [Fact]
    public async Task Get_MoreThanTwentyEvents_ReturnsOnlyTheTwentyMostRecent()
    {
        var client = Visiting();
        var email = await ActiveUser.CreateAsync(client, Factory);
        string? lastToken = null;

        for (var attempt = 0; attempt < 21; attempt++)
        {
            lastToken = (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken;
            Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var response = await GetAsync(lastToken!);

        response.Items.Should().HaveCount(20);
        response.Items.Should().OnlyContain(item => item.Event == nameof(AccountEventType.SignInSucceeded));
    }

    // AC6: no token, same 401 as any other /account/* route.
    [Fact]
    public async Task Get_WithoutToken_IsUnauthorized()
    {
        using var response = await Client().GetAsync(MyAccountEvents);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<AccountEventPageResponse> GetAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MyAccountEvents);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AccountEventPageResponse>(AppJson.Options))!;
    }
}
