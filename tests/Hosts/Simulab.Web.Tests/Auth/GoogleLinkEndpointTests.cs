using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Simulab.Identity.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// F-29 BR2 on the Web host: what the callback does with a link attempt. The three refusals here are the
/// Web's own — the Api knows nothing of the link ticket — so none of them ever reaches the Api.
/// </summary>
public sealed class GoogleLinkEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string ClientId = "simulab-test.apps.googleusercontent.com";

    private readonly FakeAuthApi _api = new();

    private readonly InMemoryWebSessionStore _store = new();

    // AC7: no marker means this is an F-20 sign-in, whatever tickets are lying around.
    [Fact]
    public async Task Complete_NoLinkMarker_SignsInAndLinksNothing()
    {
        await using var host = GoogleOn("google-id-token");
        var (client, userId) = await SignedInAsync(host);
        // A ticket exists, and is still not what decides: the marker is.
        host.Services.GetRequiredService<GoogleLinkTickets>().Issue(new GoogleLinkTicket(userId));

        var location = await CompleteAsync(client);

        location.Should().StartWith("/account/sign-in-complete?ticket=");
        _api.LinkCalls.Should().BeEmpty("a sign-in never asks the Api to link");
    }

    // AC7b: the ticket is gone — a Web restart empties them. "Start again", not a security refusal.
    [Fact]
    public async Task Complete_MarkerWithNoTicket_SaysExpiredAndLinksNothing()
    {
        await using var host = GoogleOn("google-id-token", ticketId: "a-ticket-nobody-issued");
        var (client, _) = await SignedInAsync(host);

        var location = await CompleteAsync(client);

        location.Should().Be(SecurityWith(IdentityErrorCodes.GoogleLinkExpired));
        _api.LinkCalls.Should().BeEmpty();
    }

    // AC8: the ticket belongs to another account — the session changed under the round trip.
    [Fact]
    public async Task Complete_TicketMintedForAnotherUser_SaysSessionChangedAndLinksNothing()
    {
        await using var host = GoogleOn("google-id-token");
        var (client, _) = await SignedInAsync(host);
        var ticket = host.Services.GetRequiredService<GoogleLinkTickets>().Issue(new GoogleLinkTicket(Guid.NewGuid()));
        UseLinkAttempt(host, ticket);

        var location = await CompleteAsync(client);

        location.Should().Be(SecurityWith(IdentityErrorCodes.GoogleLinkSessionChanged));
        _api.LinkCalls.Should().BeEmpty();
    }

    // AC9: a ticket is good once. The second callback with the same id finds nothing.
    [Fact]
    public async Task Complete_TicketPresentedTwice_IsRefusedTheSecondTime()
    {
        await using var host = GoogleOn("google-id-token");
        var (client, userId) = await SignedInAsync(host);
        var ticket = host.Services.GetRequiredService<GoogleLinkTickets>().Issue(new GoogleLinkTicket(userId));
        UseLinkAttempt(host, ticket);

        var first = await CompleteAsync(client);
        var second = await CompleteAsync(client);

        first.Should().Be($"{GoogleAccountEndpoints.SecurityPath}?linked=1", "the first attempt is the real one");
        second.Should().Be(SecurityWith(IdentityErrorCodes.GoogleLinkExpired));
        _api.LinkCalls.Should().ContainSingle("the Api is asked exactly once");
    }

    // AC10: the link start is a POST, and a POST with no antiforgery token never reaches Google.
    [Fact]
    public async Task StartLink_WithoutTheAntiforgeryToken_IsRefusedBeforeGoogle()
    {
        await using var host = GoogleOn(idToken: null);
        var (client, _) = await SignedInAsync(host);

        using var response = await client.PostAsync(
            GoogleAccountEndpoints.StartPath,
            new FormUrlEncodedContent([new KeyValuePair<string, string>("intent", GoogleAccountEndpoints.LinkIntent)]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Location.Should().BeNull("nothing was redirected to Google");
    }

    // AC11: and an anonymous caller cannot start one at all.
    [Fact]
    public async Task StartLink_Anonymous_IsRefused()
    {
        await using var host = GoogleOn(idToken: null);
        var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.PostAsync(
            GoogleAccountEndpoints.StartPath,
            new FormUrlEncodedContent([new KeyValuePair<string, string>("intent", GoogleAccountEndpoints.LinkIntent)]));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location!.ToString().Should().NotContain("accounts.google.com", "an anonymous caller never reaches Google");
        }
    }

    private static string SecurityWith(string code) =>
        $"{GoogleAccountEndpoints.SecurityPath}?error={Uri.EscapeDataString(code)}";

    private static async Task<string> CompleteAsync(HttpClient client)
    {
        using var response = await client.GetAsync(GoogleAccountEndpoints.CompletePath);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
        return response.Headers.Location!.ToString();
    }

    /// <summary>Puts the marker and the ticket where the challenge's protected properties would have put them.</summary>
    private static void UseLinkAttempt(WebApplicationFactory<Program> host, string ticketId)
    {
        var answer = host.Services.GetRequiredService<StandInGoogleAnswer>();
        answer.Items[GoogleAccountEndpoints.LinkIntentItem] = GoogleAccountEndpoints.LinkIntent;
        answer.Items[GoogleAccountEndpoints.LinkTicketItem] = ticketId;
    }

    /// <summary>A client holding a real auth cookie, and the account id inside it.</summary>
    private static async Task<(HttpClient Client, Guid UserId)> SignedInAsync(WebApplicationFactory<Program> host)
    {
        var userId = Guid.NewGuid();
        var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

        var ticket = host.Services.GetRequiredService<SignInTicketStore>().Issue(new SignInTicket(
            userId.ToString(), "ana@example.com", DisplayName: null, "jti-1", "access-1", "refresh-1",
            TimeSpan.FromMinutes(15), []));
        using var response = await client.GetAsync($"/account/sign-in-complete?ticket={ticket}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return (client, userId);
    }

    /// <summary>
    /// The host with the feature on, Google's discovery document given in advance, and the external cookie
    /// answered by the stand-in — optionally carrying a link attempt's marker and ticket id.
    /// </summary>
    private WebApplicationFactory<Program> GoogleOn(string? idToken, string? ticketId = null)
    {
        var answer = new StandInGoogleAnswer(idToken);
        if (ticketId is not null)
        {
            answer.Items[GoogleAccountEndpoints.LinkIntentItem] = GoogleAccountEndpoints.LinkIntent;
            answer.Items[GoogleAccountEndpoints.LinkTicketItem] = ticketId;
        }

        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Identity:GoogleSignInEnabled", "true");
            builder.UseSetting("Authentication:Google:ClientId", ClientId);
            builder.UseSetting("Authentication:Google:ClientSecret", "google-test-secret");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IWebSessionStore>(_store);
                services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
                services.AddHttpClient<IdentityApiClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
                services.PostConfigure<OpenIdConnectOptions>(GoogleSignInSettings.Scheme, options =>
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = GoogleSignInSettings.Authority,
                        AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
                        TokenEndpoint = "https://oauth2.googleapis.com/token",
                    });
                services.AddSingleton(answer);
                services.PostConfigure<AuthenticationOptions>(options =>
                    options.SchemeMap[GoogleSignInSettings.ExternalScheme].HandlerType = typeof(StandInExternalHandler));
            });
        });
    }

    public void Dispose() => _api.Dispose();
}
