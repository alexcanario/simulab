using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simulab.Web.Components;
using Simulab.Web.Components.Layout;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-85 AC15 [sec] and AC20 through the real router (`Routes.razor`), not the gate alone: removing the gate from the
/// router, or binding the route view to the route instead of the allowed route, fails these (review finding 1).
/// </summary>
public sealed class LegalAcceptanceRoutesTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public LegalAcceptanceRoutesTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(
            WebSessionId,
            new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)))
            .GetAwaiter().GetResult();
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"));
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private string CurrentPath => new Uri(Navigation.Uri).PathAndQuery;

    private IRenderedComponent<Routes> RenderRoutesAt(string path)
    {
        Navigation.NavigateTo(path);
        return Render<Routes>(parameters => parameters.Add(routes => routes.Preferences, ShellPreferences.Default));
    }

    [Fact]
    public void Owing_AGatedRouteNeverRendersItsPage_AndTheUserLandsOnTheAcceptancePage()
    {
        var page = RenderRoutesAt("/account/password");

        page.WaitForAssertion(() => CurrentPath.Should().Be("/accept-terms?returnUrl=%2Faccount%2Fpassword"));
        page.WaitForAssertion(() => page.Markup.Should().Contain("Our documents changed"));
        page.Markup.Should().NotContain("Current password", "the change-password page must not render before the acceptance");
        Api.Requests.Should().NotContain(request => request.RequestUri!.AbsolutePath.EndsWith("/profile", StringComparison.Ordinal));
    }

    [Fact]
    public void Owing_AnExemptRouteOpensAsAsked()
    {
        var page = RenderRoutesAt("/privacy");

        page.WaitForAssertion(() => page.Markup.Should().Contain("Privacy policy"));
        CurrentPath.Should().Be("/privacy");
    }

    [Fact]
    public void NotOwing_TheGatedRouteRendersItsPage()
    {
        Api.LegalStatus = new Simulab.Identity.Contracts.LegalAcceptanceStatusResponse(false, []);

        var page = RenderRoutesAt("/account/password");

        page.WaitForAssertion(() => page.Markup.Should().Contain("Current password"));
        CurrentPath.Should().Be("/account/password");
    }
}
