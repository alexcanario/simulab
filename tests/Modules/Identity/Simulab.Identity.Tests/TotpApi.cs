using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OtpNet;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>The password step's answer when two-factor is on (F-11 BR9): an OAuth error carrying the challenge.</summary>
public sealed record TotpChallengeResponse(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("challenge")] string? Challenge,
    [property: JsonPropertyName("expires_in")] long? ExpiresIn,
    [property: JsonPropertyName("access_token")] string? AccessToken);

/// <summary>The F-11 routes and the code step of the token endpoint, called the way the Web calls them.</summary>
public static class TotpApi
{
    public const string Route = "/api/v1/identity/totp";

    /// <summary>A development-only key, the shape <c>Identity:TotpEncryptionKey</c> needs (32 bytes, base64).</summary>
    public static readonly string TestKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());

    /// <summary>The code an authenticator app shows for <paramref name="secret"/> at <paramref name="at"/>.</summary>
    public static string CodeAt(string secret, DateTimeOffset at) =>
        new Totp(Base32Encoding.ToBytes(secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6).ComputeTotp(at.UtcDateTime);

    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string route, string accessToken, object? body = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: AppJson.Options);
        }

        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    public static async Task<TotpStatusResponse> StatusAsync(HttpClient client, string accessToken) =>
        await ReadAsync<TotpStatusResponse>(await SendAsync(client, HttpMethod.Get, Route, accessToken));

    public static async Task<TotpEnrolmentResponse> StartAsync(HttpClient client, string accessToken) =>
        await ReadAsync<TotpEnrolmentResponse>(await SendAsync(client, HttpMethod.Post, Route + "/enrolments", accessToken));

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string accessToken, string? code) =>
        SendAsync(client, HttpMethod.Post, Route + "/enrolments/confirmations", accessToken, new ConfirmTotpRequest(code));

    public static Task<HttpResponseMessage> RegenerateAsync(HttpClient client, string accessToken, string? code) =>
        SendAsync(client, HttpMethod.Post, Route + "/recovery-codes", accessToken, new RegenerateRecoveryCodesRequest(code));

    public static Task<HttpResponseMessage> DisableAsync(HttpClient client, string accessToken, string? password, string? code) =>
        SendAsync(client, HttpMethod.Delete, Route, accessToken, new DisableTotpRequest(password, code));

    /// <summary>The password step, read with the extra fields of the two-factor answer.</summary>
    public static async Task<TotpChallengeResponse> PasswordStepAsync(HttpClient client, string email, string password)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = email,
            ["password"] = password,
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
        }));
        return (await response.Content.ReadFromJsonAsync<TotpChallengeResponse>())!;
    }

    /// <summary>The code step: the custom <c>totp</c> grant.</summary>
    public static async Task<TokenResponse> CodeStepAsync(HttpClient client, string? challenge, string? code)
    {
        ArgumentNullException.ThrowIfNull(client);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "totp",
            ["client_id"] = TestClient.ClientId,
            ["client_secret"] = TestClient.ClientSecret,
        };
        if (challenge is not null)
        {
            form["challenge"] = challenge;
        }

        if (code is not null)
        {
            form["code"] = code;
        }

        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }
}
