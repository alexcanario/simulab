using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// F-8 AC7-AC9 and AC11, through the real Web pipeline: the profile's language and name reach the
/// culture cookie and the auth cookie at sign-in, after a save, and from the header switch.
/// </summary>
public sealed class ProfileCultureTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string CultureCookieName = ".AspNetCore.Culture";

    private readonly FakeAuthApi _api = new();
    private readonly InMemoryWebSessionStore _store = new();

    [Fact]
    public async Task SignIn_WritesTheProfileLanguage_OverTheBrowserCookieAndHeader()
    {
        await using var host = Host();
        var client = Browser(host);
        client.DefaultRequestHeaders.Add("Cookie", $"{CultureCookieName}=c%3Den%7Cuic%3Den");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");

        var response = await client.GetAsync($"/account/sign-in-complete?ticket={IssueTicket(host, "pt-PT")}");

        CultureCookieOf(response).Should().Be("c=pt-PT|uic=pt-PT");
    }

    [Fact]
    public async Task SignIn_ThenFirstPage_RendersInTheProfileLanguage()
    {
        await using var host = Host();
        var client = CookieBrowser(host);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR");
        await client.GetAsync($"/account/sign-in-complete?ticket={IssueTicket(host, "pt-PT")}");

        var html = await client.GetStringAsync("/");

        html.Should().Contain("<html lang=\"pt-PT\"");
    }

    [Theory]
    [InlineData(null, "pt-BR", "pt-BR")]
    [InlineData("c%3Dpt-PT%7Cuic%3Dpt-PT", "pt-BR", "pt-PT")]
    [InlineData(null, "fr", "en")]
    public async Task Anonymous_CultureComesFromTheCookieThenTheBrowserThenEnglish(string? cookie, string acceptLanguage, string expected)
    {
        await using var host = Host();
        var client = Browser(host);
        if (cookie is not null)
        {
            client.DefaultRequestHeaders.Add("Cookie", $"{CultureCookieName}={cookie}");
        }

        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);

        var html = await client.GetStringAsync("/");

        html.Should().Contain($"<html lang=\"{expected}\"");
    }

    [Fact]
    public async Task LanguageSwitch_SignedIn_SavesThePreferredLanguageAndSetsTheCookie()
    {
        await using var host = Host();
        var client = await SignedInAsync(host);

        var response = await client.GetAsync("/culture/set?culture=pt-BR&redirectUri=%2Faccount");

        _api.SavedLanguages.Should().Equal("pt-BR");
        CultureCookieOf(response).Should().Be("c=pt-BR|uic=pt-BR");
        response.Headers.Location!.OriginalString.Should().Be("/account");
    }

    [Fact]
    public async Task LanguageSwitch_ProfileSaveFails_StillSetsTheCookie()
    {
        _api.PreferredLanguageStatus = HttpStatusCode.InternalServerError;
        await using var host = Host();
        var client = await SignedInAsync(host);

        var response = await client.GetAsync("/culture/set?culture=pt-BR&redirectUri=%2F");

        _api.SavedLanguages.Should().ContainSingle();
        CultureCookieOf(response).Should().Be("c=pt-BR|uic=pt-BR");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task LanguageSwitch_Anonymous_OnlySetsTheCookie()
    {
        await using var host = Host();

        var response = await Browser(host).GetAsync("/culture/set?culture=pt-PT&redirectUri=%2F");

        _api.SavedLanguages.Should().BeEmpty();
        CultureCookieOf(response).Should().Be("c=pt-PT|uic=pt-PT");
    }

    [Fact]
    public async Task ProfileApplied_RewritesTheNameAndTheCulture_ThenReturnsInsideTheApp()
    {
        _api.Profile = new ProfileResponse("ana@example.com", "Ana Souza", "pt-PT");
        await using var host = Host();
        var client = await SignedInAsync(host);

        var response = await client.GetAsync("/account/profile-applied?redirectUri=%2Faccount%3Fsaved%3Dtrue");

        response.Headers.Location!.OriginalString.Should().Be("/account?saved=true");
        CultureCookieOf(response).Should().Be("c=pt-PT|uic=pt-PT");
        var claims = AuthClaimsOf(host, response);
        claims.Single(claim => claim.Type == ClaimTypes.Name).Value.Should().Be("Ana Souza");
        claims.Should().Contain(claim => claim.Type == WebAuthClaims.WebSessionId);
    }

    [Fact]
    public async Task ProfileApplied_ExternalRedirect_GoesHome()
    {
        await using var host = Host();
        var client = await SignedInAsync(host);

        var response = await client.GetAsync("/account/profile-applied?redirectUri=https%3A%2F%2Fevil.example");

        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
            services.AddHttpClient<IdentityApiClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
        }));

    private static HttpClient Browser(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false, BaseAddress = new Uri("https://localhost") });

    private static HttpClient CookieBrowser(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });

    private static async Task<HttpClient> SignedInAsync(WebApplicationFactory<Program> host)
    {
        var client = CookieBrowser(host);
        var response = await client.GetAsync($"/account/sign-in-complete?ticket={IssueTicket(host, "en")}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return client;
    }

    private static string IssueTicket(WebApplicationFactory<Program> host, string preferredLanguage) =>
        host.Services.GetRequiredService<SignInTicketStore>().Issue(new SignInTicket(
            Guid.NewGuid().ToString(), "ana@example.com", "Ana", "jti-1", "access-1", "refresh-1",
            TimeSpan.FromMinutes(15), [], preferredLanguage));

    private static string? CultureCookieOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Where(value => value.StartsWith(CultureCookieName + "=", StringComparison.Ordinal))
                .Select(value => Uri.UnescapeDataString(value[(CultureCookieName.Length + 1)..value.IndexOf(';', StringComparison.Ordinal)]))
                .SingleOrDefault()
            : null;

    private static List<Claim> AuthClaimsOf(WebApplicationFactory<Program> host, HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("simulab.auth=", StringComparison.Ordinal));
        var protectedTicket = cookie["simulab.auth=".Length..cookie.IndexOf(';', StringComparison.Ordinal)];
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        return options.TicketDataFormat.Unprotect(protectedTicket)!.Principal.Claims.ToList();
    }

    public void Dispose() => _api.Dispose();
}
