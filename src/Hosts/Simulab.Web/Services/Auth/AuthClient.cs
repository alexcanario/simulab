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
    string? ErrorDescription,
    string? Challenge = null,
    string? GoogleEmail = null,
    string? GoogleName = null)
{
    public static TokenResult Failed(string code, string? description, string? challenge = null) => new(false, null, null, null, code, description, challenge);

    /// <summary>F-11 BR9: the password was right and the account asks for a code; <see cref="Challenge"/> carries the attempt on.</summary>
    public bool NeedsTotpCode => ErrorCode == IdentityErrorCodes.TotpRequired && !string.IsNullOrEmpty(Challenge);

    /// <summary>F-53 BR3: the right password (and code) of a marked account; <see cref="Challenge"/> carries the attempt to the new-password step.</summary>
    public bool NeedsPasswordChange => ErrorCode == IdentityErrorCodes.PasswordChangeRequired && !string.IsNullOrEmpty(Challenge);

    /// <summary>F-20 BR7: Google knows this person and Simulab does not yet; the confirmation page takes over.</summary>
    public bool NeedsGoogleSignUp => ErrorCode == IdentityErrorCodes.GoogleSignUpRequired && !string.IsNullOrEmpty(GoogleEmail);

    /// <summary>
    /// The token endpoint refused this refresh token itself (B-3, BR3): only then does a web session end.
    /// Any other error (<c>server_error</c>, <c>invalid_client</c> after a config change, no answer) is not
    /// the session's fault and never signs anyone out.
    /// </summary>
    public bool IsRejected => ErrorCode is IdentityErrorCodes.RefreshTokenInvalid or "invalid_grant";
}

file sealed record TokenResponseBody(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription,
    [property: JsonPropertyName("challenge")] string? Challenge,
    [property: JsonPropertyName(GoogleSignInProtocol.EmailParameter)] string? Email,
    [property: JsonPropertyName(GoogleSignInProtocol.NameParameter)] string? Name);

/// <summary>
/// Talks to the Api's OpenIddict token endpoint the way BR1 describes it: form-encoded, with the
/// confidential client's own credentials, never the browser (F-5, decision 2).
/// </summary>
public sealed class AuthClient(HttpClient http, IOptions<OpenIddictClientOptions> options)
{
    public Task<TokenResult> SignInAsync(string email, string password, string? visitorAddress = null, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = password,
            },
            cancellationToken,
            visitorAddress);

    /// <summary>F-11 BR9: the code step, the custom <c>totp</c> grant. The challenge is spent whatever the answer.</summary>
    public Task<TokenResult> CompleteTotpSignInAsync(string challenge, string code, string? visitorAddress = null, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "totp",
                ["challenge"] = challenge,
                ["code"] = code,
            },
            cancellationToken,
            visitorAddress);

    /// <summary>F-53 BR4: the new-password step, the custom <c>password_change</c> grant. Signs the user in when it succeeds.</summary>
    public Task<TokenResult> CompletePasswordChangeAsync(string challenge, string newPassword, string? visitorAddress = null, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password_change",
                ["challenge"] = challenge,
                ["new_password"] = newPassword,
            },
            cancellationToken,
            visitorAddress);

    /// <summary>F-20: the Google step, the custom <c>google</c> grant. The Api checks the ID token itself (BR2).</summary>
    public Task<TokenResult> SignInWithGoogleAsync(string idToken, CancellationToken cancellationToken = default) =>
        RequestAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = GoogleSignInProtocol.GrantType,
                [GoogleSignInProtocol.IdTokenParameter] = idToken,
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
            using var response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
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
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
        {
            return new SessionLookup(SessionLookupStatus.Unavailable, null);
        }
    }

    private async Task<TokenResult> RequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken, string? visitorAddress = null)
    {
        form["client_id"] = options.Value.ClientId;
        form["client_secret"] = options.Value.ClientSecret;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token") { Content = new FormUrlEncodedContent(form) };

            // F-38 BR6, B-4: the password and code steps of the sign-in page carry the visitor's address and the
            // proof that it comes from the Web, so the Api's per-address limit counts the visitor, not this server.
            if (!string.IsNullOrEmpty(visitorAddress) && !string.IsNullOrEmpty(options.Value.ClientSecret))
            {
                request.Headers.Add(ClientAddressHeaders.Address, visitorAddress);
                request.Headers.Add(ClientAddressHeaders.Secret, options.Value.ClientSecret);
            }

            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<TokenResponseBody>(cancellationToken: cancellationToken);

            if (body?.Error is not null)
            {
                return TokenResult.Failed(body.Error, body.ErrorDescription, body.Challenge) with { GoogleEmail = body.Email, GoogleName = body.Name };
            }

            // A 5xx comes back as problem details with no OAuth "error": that is no answer, not a token pair.
            return response.IsSuccessStatusCode && body?.AccessToken is not null && body.RefreshToken is not null
                ? new TokenResult(true, body.AccessToken, body.RefreshToken, body.ExpiresIn, null, null)
                : TokenResult.Failed(Components.Ui.ErrorText.UnexpectedCode, null);
        }
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
        {
            return TokenResult.Failed(Components.Ui.ErrorText.UnexpectedCode, null);
        }
    }

    /// <summary>
    /// Anything that means "the Api did not give a usable answer": network, timeout, the resilience
    /// handler's own rejections, a body that is not JSON. A cancellation the caller asked for is not one.
    /// </summary>
    private static bool IsTransportFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;
}
