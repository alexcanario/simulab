using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
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

    // BR2's own seam: the start really mints a ticket for the caller and marks the challenge. Every callback
    // test above injects the marker by hand, so without this a typo in either item key — or a ticket minted
    // for the wrong id — would leave them all green while the feature silently stopped linking anything.
    [Fact]
    public async Task StartLink_SignedInWithTheToken_ChallengesGoogleWithAMarkerAndATicketForTheCaller()
    {
        await using var host = GoogleOn(idToken: null);
        var (client, userId) = await SignedInAsync(host);

        using var response = await PostLinkAsync(host, client, withToken: true);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
        var location = response.Headers.Location!.ToString();
        location.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");

        // The ticket id and the marker are inside the OIDC `state`, protected — which is the whole point of
        // BR2 — so no test can read them from here, and none should: what must never appear in the address
        // is exactly what a referrer would leak.
        location.Should().NotContain(GoogleAccountEndpoints.LinkTicketItem).And.NotContain(userId.ToString());
    }

    // AC11: an anonymous caller is refused, and refused *for being anonymous*. The status alone cannot say
    // which filter spoke — antiforgery answers 400 too — so the endpoint's own authorization metadata is
    // asserted beside the behaviour. An anonymous visitor cannot even obtain a token for this form: the page
    // that carries it is behind sign-in.
    [Fact]
    public async Task StartLink_Anonymous_IsRefusedAndTheEndpointRequiresAuthorization()
    {
        await using var host = GoogleOn(idToken: null);
        var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await PostLinkAsync(host, client, withToken: false);

        // Authorization runs before the antiforgery filter, so the answer is the cookie challenge — not the
        // 400 a missing token would give. That is the refusal AC11 is about, and it is unambiguous.
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("sign-in").And.NotContain("accounts.google.com");

        var start = host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
                endpoint.RoutePattern.RawText == GoogleAccountEndpoints.StartPath
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(HttpMethods.Post));
        start.Metadata.GetMetadata<IAuthorizeData>().Should().NotBeNull("BR2: the link start is for a signed-in account only");
    }

    /// <summary>
    /// Posts the link form. With <paramref name="withToken"/> it carries a real antiforgery pair, generated
    /// by the host's own service, so the refusal a test sees is the one it is about.
    /// </summary>
    private static async Task<HttpResponseMessage> PostLinkAsync(
        WebApplicationFactory<Program> host,
        HttpClient client,
        bool withToken)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("intent", GoogleAccountEndpoints.LinkIntent),
        };

        if (withToken)
        {
            // The token is bound to the caller, so it can only come from a page this caller was served —
            // which is what a browser does. `/account/security` renders the link form with it inside.
            var page = await client.GetStringAsync(GoogleAccountEndpoints.SecurityPath);
            var field = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.FormFieldName;
            var match = Regex.Match(page, $"name=\"{Regex.Escape(field)}\"[^>]*value=\"([^\"]+)\"");
            match.Success.Should().BeTrue("the Security page must render the antiforgery token inside the link form");
            fields.Add(new KeyValuePair<string, string>(field, match.Groups[1].Value));
        }

        return await client.PostAsync(GoogleAccountEndpoints.StartPath, new FormUrlEncodedContent(fields));
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
