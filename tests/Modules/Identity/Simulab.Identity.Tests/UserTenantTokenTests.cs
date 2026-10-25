using System.Net;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-150: the <c>tenant_id</c> claim is the stored tenant at every sign-in and refresh (AC9), whatever a
/// request sends (AC16), and an account with a tenant can sign in and refresh at all (AC23).
/// </summary>
public sealed class UserTenantTokenTests : UserTenantApiTests
{
    // AC9, AC23.
    [Fact]
    public async Task SignInAndRefresh_CarryTheTenantStoredAtThatMoment()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, first));

        var signedIn = await TokenClient.SignInAsync(Client(), user.Email!, Accounts.ValidPassword);
        signedIn.AccessToken.Should().NotBeNull(signedIn.ErrorDescription);
        (await TenantClaimAsync(signedIn.AccessToken!)).Should().Be(first.ToString());

        await ReadUserAsync(await PutTenantAsync(admin, user.Id, second));
        var refreshed = await TokenClient.RefreshAsync(Client(), signedIn.RefreshToken!);
        refreshed.AccessToken.Should().NotBeNull(refreshed.ErrorDescription);
        (await TenantClaimAsync(refreshed.AccessToken!)).Should().Be(second.ToString());

        await ReadUserAsync(await PutTenantAsync(admin, user.Id, null));
        var cleared = await TokenClient.RefreshAsync(Client(), refreshed.RefreshToken!);
        cleared.AccessToken.Should().NotBeNull(cleared.ErrorDescription);
        // "Empty when there is none": OpenIddict drops a claim whose value is empty, so the token has no tenant at all.
        (await TenantClaimAsync(cleared.AccessToken!)).Should().BeNullOrEmpty();
    }

    // AC16.
    [Fact]
    public async Task TokenRequestsThatSendATenantOfTheirOwn_StillCarryTheStoredOne()
    {
        var user = await Accounts.CreateAsync(Factory.Services);
        var stored = Guid.CreateVersion7();
        var forged = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, stored);

        var signedIn = await RequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = user.Email!,
            ["password"] = Accounts.ValidPassword,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
            [SessionClaims.TenantId] = forged.ToString()
        });
        var refreshed = await RequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = signedIn.RefreshToken!,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
            [SessionClaims.TenantId] = forged.ToString()
        });

        (await TenantClaimAsync(signedIn.AccessToken!)).Should().Be(stored.ToString());
        (await TenantClaimAsync(refreshed.AccessToken!)).Should().Be(stored.ToString());

        // The session endpoint answers from the token and never echoes a tenant a request sends.
        var session = Bearer(Client(), refreshed.AccessToken!);
        var answer = await session.GetAsync($"/api/v1/identity/session?{SessionClaims.TenantId}={forged}");
        answer.StatusCode.Should().Be(HttpStatusCode.OK);
        (await answer.Content.ReadAsStringAsync()).Should().NotContain(forged.ToString());
    }

    private async Task<TokenResponse> RequestAsync(Dictionary<string, string> form)
    {
        using var response = await Client().PostAsync("/connect/token", new FormUrlEncodedContent(form));
        var token = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<TokenResponse>(response.Content))!;
        token.AccessToken.Should().NotBeNull(token.ErrorDescription);
        return token;
    }
}
