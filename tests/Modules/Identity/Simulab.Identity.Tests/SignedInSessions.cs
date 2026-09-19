using System.Net;
using System.Net.Http.Headers;

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
    public static async Task<HttpStatusCode> StatusOfAsync(HttpClient client, TokenResponse session)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(session);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
