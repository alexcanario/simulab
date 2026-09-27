using System.Net.Http.Headers;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-29: the three routes of the signed-in account's own Google link, called the way the Web calls them.</summary>
public static class GoogleLinkApi
{
    public const string LinksRoute = "/api/v1/identity/google-links";

    public const string RemovalsRoute = "/api/v1/identity/google-link-removals";

    public static Task<HttpResponseMessage> GetAsync(HttpClient client, string? accessToken) =>
        SendAsync(client, HttpMethod.Get, LinksRoute, accessToken);

    public static Task<HttpResponseMessage> LinkAsync(HttpClient client, string? accessToken, string? idToken) =>
        SendAsync(client, HttpMethod.Post, LinksRoute, accessToken, new GoogleLinkRequest(idToken));

    public static Task<HttpResponseMessage> UnlinkAsync(HttpClient client, string? accessToken, string? currentPassword) =>
        SendAsync(client, HttpMethod.Post, RemovalsRoute, accessToken, new GoogleLinkRemovalRequest(currentPassword));

    /// <summary>The link state the Security page reads.</summary>
    public static async Task<GoogleLinkResponse> StateAsync(HttpClient client, string? accessToken)
    {
        using var response = await GetAsync(client, accessToken);
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<GoogleLinkResponse>(AppJson.Options))!;
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string route,
        string? accessToken,
        object? body = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new HttpRequestMessage(method, route);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: AppJson.Options);
        }

        return client.SendAsync(request);
    }
}
