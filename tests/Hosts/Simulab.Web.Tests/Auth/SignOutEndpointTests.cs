using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR5 and BR6: the plain sign-out endpoint, through the real Web pipeline.</summary>
public sealed class SignOutEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly FakeAuthApi _api = new();
    private readonly InMemoryWebSessionStore _store = new();

    [Fact]
    public async Task SignOut_UsesTheStoredTokenRevokesAtTheApiAndRemovesTheEntry()
    {
        await using var host = Host();
        var client = await SignedInClientAsync(host);

        var response = await client.GetAsync("/account/sign-out");

        response.Headers.Location!.OriginalString.Should().Be("/");
        _api.SignOutCount.Should().Be(1);
        _store.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task SignOut_SessionEnded_SkipsTheApiAndGoesToSignInWithTheAlert()
    {
        await using var host = Host();
        var client = await SignedInClientAsync(host);

        var response = await client.GetAsync($"/account/sign-out?reason={AccountEndpoints.SessionEndedReason}");

        response.Headers.Location!.OriginalString.Should().Be(AccountEndpoints.SessionEndedSignInPath);
        _api.SignOutCount.Should().Be(0);
        _store.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task SignOut_ApiFails_StillRemovesTheEntryAndClearsTheCookie()
    {
        _api.SignOutStatus = HttpStatusCode.InternalServerError;
        await using var host = Host();
        var client = await SignedInClientAsync(host);

        var response = await client.GetAsync("/account/sign-out");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        _store.Sessions.Should().BeEmpty();
        response.Headers.GetValues("Set-Cookie").Should().Contain(value => value.StartsWith("simulab.auth=;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SignInAgain_OverALiveCookie_LeavesNoOrphanEntry()
    {
        await using var host = Host();
        var client = await SignedInClientAsync(host);

        await client.GetAsync($"/account/sign-in-complete?ticket={IssueTicket(host)}");

        _store.Sessions.Should().ContainSingle();
    }

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
        }));

    /// <summary>A client holding a real auth cookie, obtained the way the sign-in page obtains it.</summary>
    private static async Task<HttpClient> SignedInClientAsync(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync($"/account/sign-in-complete?ticket={IssueTicket(host)}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return client;
    }

    private static string IssueTicket(WebApplicationFactory<Program> host) =>
        host.Services.GetRequiredService<SignInTicketStore>().Issue(new SignInTicket(
            Guid.NewGuid().ToString(), "ana@example.com", DisplayName: null, "jti-1", "access-1", "refresh-1",
            TimeSpan.FromMinutes(15), []));

    public void Dispose() => _api.Dispose();
}
