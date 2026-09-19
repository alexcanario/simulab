using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Simulab.Identity.Tests;

/// <summary>Signs one account in several times, as several devices would, and says which of those sessions still work.</summary>
public static class SignedInSessions
{
    public static async Task<IReadOnlyList<TokenResponse>> CreateAsync(HttpClient client, string email, string password, int count)
    {
        var sessions = new List<TokenResponse>();
        for (var i = 0; i < count; i++)
        {
            var token = await TokenClient.SignInAsync(client, email, password);
            token.AccessToken.Should().NotBeNull(token.ErrorDescription);
            sessions.Add(token);
        }

        return sessions;
    }

    /// <summary>Calls a signed-in endpoint with the session's access token: 200 while it lives, 401 once revoked.</summary>
    public static async Task<HttpStatusCode> StatusOfAsync(HttpClient client, TokenResponse session) =>
        (await AnswerOfAsync(client, session.AccessToken!)).Status;

    /// <summary>The status and, when there is one, the problem <c>code</c> a signed-in call with this access token gets.</summary>
    public static async Task<(HttpStatusCode Status, string? Code)> AnswerOfAsync(HttpClient client, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        string? code = null;
        if (!response.IsSuccessStatusCode && body.Length > 0
            && System.Text.Json.JsonDocument.Parse(body).RootElement.TryGetProperty("code", out var element))
        {
            code = element.GetString();
        }

        return (response.StatusCode, code);
    }

    /// <summary>The session id the Api put in this access token, read back the way the Web reads it.</summary>
    public static async Task<string> SessionJtiAsync(HttpClient client, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        var session = await response.Content.ReadFromJsonAsync<Simulab.Identity.Contracts.SessionInfoResponse>(Simulab.SharedKernel.Serialization.AppJson.Options);
        return session!.SessionJti;
    }
}
