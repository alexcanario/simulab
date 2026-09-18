using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Simulab.Web.Services.Auth;

/// <summary>The OAuth2 token response shape (not RFC 9457: this is OpenIddict's own protocol endpoint).</summary>
public sealed record TokenResult(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    int? ExpiresInSeconds,
    string? ErrorCode,
    string? ErrorDescription)
{
    public static TokenResult Failed(string code, string? description) => new(false, null, null, null, code, description);
}

file sealed record TokenResponseBody(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription);

/// <summary>
/// Talks to the Api's OpenIddict token endpoint the way BR1 describes it: form-encoded, with the
/// confidential client's own credentials, never the browser (F-5, decision 2).
/// </summary>
public sealed class AuthClient(HttpClient http, IOptions<OpenIddictClientOptions> options)
{
    public Task<TokenResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = password,
            },
            cancellationToken);

    public Task<TokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            },
            cancellationToken);

    /// <summary>BR6: revokes the caller's own session. A network failure here is not fatal: the cookie still clears.</summary>
    public async Task SignOutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sign-out");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The session still expires on its own (BR4); losing this call only delays the revocation.
        }
    }

    private async Task<TokenResult> RequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = options.Value.ClientId;
        form["client_secret"] = options.Value.ClientSecret;

        using var response = await http.PostAsync("/connect/token", new FormUrlEncodedContent(form), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TokenResponseBody>(cancellationToken: cancellationToken);

        return body is { Error: null }
            ? new TokenResult(true, body.AccessToken, body.RefreshToken, body.ExpiresIn, null, null)
            : TokenResult.Failed(body?.Error ?? Components.Ui.ErrorText.UnexpectedCode, body?.ErrorDescription);
    }
}
