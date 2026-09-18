using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Simulab.Identity.Tests;

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription);

/// <summary>Posts to the OpenIddict token endpoint the way the Web does: form-encoded, with the client credentials.</summary>
public static class TokenClient
{
    public static Task<TokenResponse> SignInAsync(HttpClient client, string email, string password) =>
        RequestAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
        });

    public static Task<TokenResponse> RefreshAsync(HttpClient client, string refreshToken) =>
        RequestAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
        });

    private static async Task<TokenResponse> RequestAsync(HttpClient client, Dictionary<string, string> form)
    {
        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }
}
