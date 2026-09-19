using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR4 and BR7: the auth cookie is checked against the session on every request, through the options Program really configures.</summary>
public sealed class AuthCookieValidationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string WebSessionId = "web-1";

    private readonly FakeAuthApi _api = new();
    private readonly InMemoryWebSessionStore _store = new();

    private WebApplicationFactory<Program> Host() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IWebSessionStore>(_store);
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
        }));

    private Task StoreLiveSession(IReadOnlyList<string>? permissions = null) =>
        _store.SaveAsync(WebSessionId, new WebSession(
            "jti-0", "access-0", "refresh-0", DateTimeOffset.UtcNow.AddMinutes(10), permissions ?? [], DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30)));

    [Fact]
    public async Task ValidatePrincipal_RevokedSession_RejectsPrincipal()
    {
        await StoreLiveSession();
        _api.SessionStatus = HttpStatusCode.Unauthorized;
        await using var host = Host();

        var context = await ValidateAsync(host.Services, SignedInPrincipal(WebSessionId), pageLoad: true);

        context.Principal.Should().BeNull();
        _store.Sessions.Should().NotContainKey(WebSessionId);
    }

    [Fact]
    public async Task ValidatePrincipal_PermissionsChanged_PrincipalCarriesNewPermissions()
    {
        await StoreLiveSession(permissions: []);
        _api.Permissions = ["identity.roles.manage"];
        await using var host = Host();

        var context = await ValidateAsync(host.Services, SignedInPrincipal(WebSessionId), pageLoad: true);

        context.Principal!.FindAll(WebAuthClaims.Permission).Select(claim => claim.Value).Should().Equal("identity.roles.manage");
        context.Principal.FindFirstValue(ClaimTypes.Email).Should().Be("ana@example.com");
    }

    [Fact]
    public async Task ValidatePrincipal_CookieFromBeforeTheFix_RejectsPrincipal()
    {
        await using var host = Host();

        var context = await ValidateAsync(host.Services, SignedInPrincipal(webSessionId: null), pageLoad: true);

        context.Principal.Should().BeNull();
    }

    [Fact]
    public async Task ValidatePrincipal_AssetRequestRecentlyChecked_DoesNotCallApi()
    {
        await _store.SaveAsync(WebSessionId, new WebSession("jti-0", "access-0", "refresh-0", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        await using var host = Host();

        var context = await ValidateAsync(host.Services, SignedInPrincipal(WebSessionId), pageLoad: false);

        context.Principal.Should().NotBeNull();
        _api.SessionCount.Should().Be(0);
    }

    private static ClaimsPrincipal SignedInPrincipal(string? webSessionId)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, "ana@example.com"));
        if (webSessionId is not null)
        {
            identity.AddClaim(new Claim(WebAuthClaims.WebSessionId, webSessionId));
        }

        return new ClaimsPrincipal(identity);
    }

    private static async Task<CookieValidatePrincipalContext> ValidateAsync(IServiceProvider services, ClaimsPrincipal principal, bool pageLoad)
    {
        await using var scope = services.CreateAsyncScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (pageLoad)
        {
            httpContext.Request.Headers["Sec-Fetch-Mode"] = "navigate";
        }

        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var schemes = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);

        var context = new CookieValidatePrincipalContext(httpContext, scheme!, options, ticket);
        await options.Events.ValidatePrincipal(context);
        return context;
    }

    public void Dispose() => _api.Dispose();
}
