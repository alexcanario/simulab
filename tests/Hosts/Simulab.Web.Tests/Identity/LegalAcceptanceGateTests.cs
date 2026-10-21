using System.Net;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-85 BR6, BR7: a signed-in user who owes an acceptance is sent to `/accept-terms` before the page they asked for
/// renders, on every navigation, except for the pages BR7 lists (AC15, AC16, AC17, AC20).
/// </summary>
public sealed class LegalAcceptanceGateTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public LegalAcceptanceGateTests()
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
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private string CurrentPath => new Uri(Navigation.Uri).PathAndQuery;

    private void SignIn() =>
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));

    private static RouteData Route(Type pageType) => new(pageType, new Dictionary<string, object?>());

    private IRenderedComponent<LegalAcceptanceGate> RenderGate(Type pageType) =>
        Render<LegalAcceptanceGate>(parameters => parameters
            .Add(gate => gate.RouteData, Route(pageType))
            .Add(gate => gate.ChildContent, (RouteData allowed) => $"<p id=\"shown\">{allowed.PageType.Name}</p>"));

    // AC15 [sec]: a gated page is never rendered, and the user is sent to the acceptance page.
    [Fact]
    public void Owing_AGatedPageIsNotRenderedAndTheUserIsSentToAcceptTerms()
    {
        SignIn();
        Navigation.NavigateTo("/exams");

        var gate = RenderGate(typeof(Home));

        gate.FindAll("#shown").Should().BeEmpty("the page asked for never renders (and so never calls the Api) before the check");
        CurrentPath.Should().Be("/accept-terms?returnUrl=%2Fexams");
    }

    // AC17: every sign-in ends at "/", and a user who owes lands on the acceptance page without a return address.
    [Fact]
    public void Owing_TheHomePage_GoesToAcceptTermsWithNoReturnUrl()
    {
        SignIn();

        RenderGate(typeof(Home));

        CurrentPath.Should().Be("/accept-terms");
    }

    // AC15 [sec]: the exempt pages open for a user who owes, and the gate does not even ask the Api.
    [Theory]
    [InlineData(typeof(AcceptTerms))]
    [InlineData(typeof(LegalDocumentPage))]
    [InlineData(typeof(Account))]
    public void Owing_AnExemptPageIsRendered(Type pageType)
    {
        SignIn();
        Navigation.NavigateTo("/somewhere");

        var gate = RenderGate(pageType);

        gate.Find("#shown").TextContent.Should().Be(pageType.Name);
        CurrentPath.Should().Be("/somewhere");
        Api.CountOf("/legal-acceptance").Should().Be(0);
    }

    // AC15 [sec], T2: exempting /account by prefix would open these two.
    [Theory]
    [InlineData(typeof(ChangePassword))]
    [InlineData(typeof(Security))]
    public void Owing_TheOtherAccountPagesStayBehindTheGate(Type pageType)
    {
        SignIn();

        var gate = RenderGate(pageType);

        gate.FindAll("#shown").Should().BeEmpty();
        CurrentPath.Should().StartWith("/accept-terms");
    }

    [Fact]
    public void NotOwing_ThePageIsRendered()
    {
        SignIn();
        Api.LegalStatus = new Simulab.Identity.Contracts.LegalAcceptanceStatusResponse(false, []);
        Navigation.NavigateTo("/exams");

        var gate = RenderGate(typeof(Home));

        gate.Find("#shown").TextContent.Should().Be("Home");
        CurrentPath.Should().Be("/exams");
    }

    [Fact]
    public void Anonymous_ThePageIsRenderedWithoutCallingTheApi()
    {
        var gate = RenderGate(typeof(Home));

        gate.Find("#shown").TextContent.Should().Be("Home");
        Api.CountOf("/legal-acceptance").Should().Be(0);
    }

    // BR2, B-3 BR3: an Api that does not answer never locks a user out.
    [Fact]
    public void TheStatusCallFails_ThePageIsRenderedAnyway()
    {
        SignIn();
        Api.LegalStatusFailure = (HttpStatusCode.InternalServerError, "identity.unexpected");

        var gate = RenderGate(typeof(Home));

        gate.Find("#shown").TextContent.Should().Be("Home");
    }

    // AC16: a version published after the page loaded is caught at the next navigation, with the shell still mounted.
    [Fact]
    public void ANewVersionIsPublishedWhileTheSessionIsOpen_TheNextNavigationIsSentToAcceptTerms()
    {
        SignIn();
        Api.LegalStatus = new Simulab.Identity.Contracts.LegalAcceptanceStatusResponse(false, []);
        var gate = RenderGate(typeof(Home));
        gate.Find("#shown").TextContent.Should().Be("Home");

        Api.LegalStatus = IdentityPageTestContext.StubApiHandler.OwingBoth(privacy: "2026-v4");
        Navigation.NavigateTo("/exams");
        gate.Render(parameters => parameters.Add(g => g.RouteData, Route(typeof(Security))));

        CurrentPath.Should().Be("/accept-terms?returnUrl=%2Fexams");
        gate.Find("#shown").TextContent.Should().Be("Home", "the last allowed page stays while the check redirects, so the shell is not torn down");
    }
}
