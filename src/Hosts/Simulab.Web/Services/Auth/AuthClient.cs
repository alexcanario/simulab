using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

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

    /// <summary>The token endpoint refused the grant itself (an OAuth error), as opposed to not answering at all.</summary>
    public bool IsRejected => !IsSuccess && ErrorCode != Components.Ui.ErrorText.UnexpectedCode;
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The session still expires on its own (BR4); losing this call only delays the revocation.
        }
    }

    /// <summary>Reads the just-issued token's own claims back from the Api (build decision: the token may be encrypted).</summary>
    public async Task<SessionInfoResponse?> GetSessionAsync(string accessToken, CancellationToken cancellationToken = default) =>
        (await LookUpSessionAsync(accessToken, cancellationToken)).Session;

    /// <summary>
    /// Asks the Api whether the session behind this access token is still alive (B-3, BR3). Only a 401 says
    /// it is not; any other failure is <see cref="SessionLookupStatus.Unavailable"/>, never a sign-out.
    /// </summary>
    public async Task<SessionLookup> LookUpSessionAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new SessionLookup(SessionLookupStatus.Ended, null);
            }

            var session = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<SessionInfoResponse>(AppJson.Options, cancellationToken)
                : null;
            return session is null
                ? new SessionLookup(SessionLookupStatus.Unavailable, null)
                : new SessionLookup(SessionLookupStatus.Alive, session);
        }
        catch (HttpRequestException)
        {
            return new SessionLookup(SessionLookupStatus.Unavailable, null);
        }
    }

    private async Task<TokenResult> RequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = options.Value.ClientId;
        form["client_secret"] = options.Value.ClientSecret;

        TokenResponseBody? body;
        try
        {
            using var response = await http.PostAsync("/connect/token", new FormUrlEncodedContent(form), cancellationToken);
            body = await response.Content.ReadFromJsonAsync<TokenResponseBody>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            return TokenResult.Failed(Components.Ui.ErrorText.UnexpectedCode, null);
        }

        return body is { Error: null }
            ? new TokenResult(true, body.AccessToken, body.RefreshToken, body.ExpiresIn, null, null)
            : TokenResult.Failed(body?.Error ?? Components.Ui.ErrorText.UnexpectedCode, body?.ErrorDescription);
    }
}
