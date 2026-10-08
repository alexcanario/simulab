using System.Net.Http.Json;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>
/// The token endpoint called the way the Web does for a visitor (B-4): the form, plus the visitor's address and the
/// Web's secret in headers. F-38 tests use a different address per scenario so no two share a counter.
/// </summary>
public static class SignInFrom
{
    public static Task<TokenResponse> PasswordAsync(HttpClient client, string? address, string email, string password, string? secret = TestClient.ClientSecret) =>
        SendAsync(client, address, secret, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
        });

    public static Task<TokenResponse> CodeAsync(HttpClient client, string? address, string? challenge, string? code, string? secret = TestClient.ClientSecret)
    {
        var form = new Dictionary<string, string> { ["grant_type"] = "totp" };
        if (challenge is not null)
        {
            form["challenge"] = challenge;
        }

        if (code is not null)
        {
            form["code"] = code;
        }

        return SendAsync(client, address, secret, form);
    }

    /// <summary>The new-password step of a marked account (F-53 BR4): the challenge the sign-in handed out, and the password to set.</summary>
    public static Task<TokenResponse> PasswordChangeAsync(HttpClient client, string? address, string? challenge, string? newPassword, string grantType = "password_change")
    {
        var form = new Dictionary<string, string> { ["grant_type"] = grantType };
        if (challenge is not null)
        {
            form["challenge"] = challenge;
        }

        if (newPassword is not null)
        {
            form["new_password"] = newPassword;
        }

        return SendAsync(client, address, TestClient.ClientSecret, form);
    }

    public static Task<TokenResponse> RefreshAsync(HttpClient client, string? address, string refreshToken) =>
        SendAsync(client, address, TestClient.ClientSecret, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

    public static Task<TokenResponse> GoogleAsync(HttpClient client, string? address, string idToken) =>
        SendAsync(client, address, TestClient.ClientSecret, new Dictionary<string, string>
        {
            ["grant_type"] = GoogleSignInProtocol.GrantType,
            [GoogleSignInProtocol.IdTokenParameter] = idToken,
        });

    /// <summary>Fails <paramref name="count"/> different account names that do not exist, from <paramref name="address"/>.</summary>
    public static async Task FailUnknownNamesAsync(HttpClient client, string address, int count, string prefix = "ghost")
    {
        for (var i = 0; i < count; i++)
        {
            var failed = await PasswordAsync(client, address, $"{prefix}-{i}@exemplo.com", "not-the-password");
            failed.Error.Should().Be(IdentityErrorCodes.InvalidCredentials, $"name {i} is still under the limit");
        }
    }

    private static async Task<TokenResponse> SendAsync(HttpClient client, string? address, string? secret, Dictionary<string, string> form)
    {
        form["client_id"] = TestClient.ClientId;
        form["client_secret"] = TestClient.ClientSecret;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token") { Content = new FormUrlEncodedContent(form) };
        if (address is not null)
        {
            request.Headers.Add(ClientAddressHeaders.Address, address);
        }

        if (secret is not null)
        {
            request.Headers.Add(ClientAddressHeaders.Secret, secret);
        }

        using var response = await client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }
}
