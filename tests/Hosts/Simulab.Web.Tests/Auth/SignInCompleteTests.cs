using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR1 and BR6: after sign-in the tokens sit on the server and the cookie carries none.</summary>
public class SignInCompleteTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SignInComplete_StoresTokensServerSide_CookieCarriesOnlyIdentityAndSession()
    {
        var store = new InMemoryWebSessionStore();
        await using var host = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IWebSessionStore>(store)));
        var ticketId = host.Services.GetRequiredService<SignInTicketStore>().Issue(new SignInTicket(
            Guid.NewGuid().ToString(), "ana@example.com", DisplayName: null, "jti-1", "access-1", "refresh-1",
            TimeSpan.FromMinutes(15), ["identity.roles.manage"]));
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/account/sign-in-complete?ticket={ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var (webSessionId, session) = store.Sessions.Single();
        session.Should().Match<WebSession>(stored =>
            stored.AccessToken == "access-1" && stored.RefreshToken == "refresh-1" && stored.ApiSessionJti == "jti-1");
        session.Permissions.Should().Equal("identity.roles.manage");

        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("simulab.auth=", StringComparison.Ordinal));
        var protectedTicket = cookie["simulab.auth=".Length..cookie.IndexOf(';', StringComparison.Ordinal)];
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = options.TicketDataFormat.Unprotect(protectedTicket)!.Principal.Claims.ToList();

        claims.Select(claim => claim.Type).Should().BeEquivalentTo(
            [System.Security.Claims.ClaimTypes.NameIdentifier, System.Security.Claims.ClaimTypes.Email, WebAuthClaims.WebSessionId]);
        claims.Single(claim => claim.Type == WebAuthClaims.WebSessionId).Value.Should().Be(webSessionId);
        claims.Select(claim => claim.Value).Should().NotContain(["access-1", "refresh-1"]);
    }
}
