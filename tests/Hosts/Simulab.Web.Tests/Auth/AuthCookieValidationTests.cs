using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR4: the auth cookie is checked against the session on every request, through the options Program really configures.</summary>
public class AuthCookieValidationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ValidatePrincipal_RevokedSession_RejectsPrincipal()
    {
        var api = new StubApiHandler(HttpStatusCode.Unauthorized);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => api)));

        var context = await ValidateAsync(host.Services, SignedInPrincipal("session-1"));

        context.Principal.Should().BeNull();
    }

    private static ClaimsPrincipal SignedInPrincipal(string sessionJti)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, "ana@example.com"));
        identity.AddClaim(new Claim(SessionClaims.SessionJti, sessionJti));
        return new ClaimsPrincipal(identity);
    }

    private static async Task<CookieValidatePrincipalContext> ValidateAsync(IServiceProvider services, ClaimsPrincipal principal)
    {
        await using var scope = services.CreateAsyncScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var schemes = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);

        var context = new CookieValidatePrincipalContext(httpContext, scheme!, options, ticket);
        await options.Events.ValidatePrincipal(context);
        return context;
    }
}

/// <summary>Answers every Api call with one status: 401 plays a revoked session (F-5 BR7).</summary>
internal sealed class StubApiHandler(HttpStatusCode status) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"error\":\"invalid_grant\"}", System.Text.Encoding.UTF8, "application/json")
        });
    }
}
