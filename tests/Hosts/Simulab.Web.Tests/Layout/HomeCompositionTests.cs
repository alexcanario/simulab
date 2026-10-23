using System.Security.Claims;
using Bunit;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-134 AC1 to AC4 (BR1, BR2, BR3, BR8): Home welcomes a visitor with the two ways in, and a signed-in user with one card
/// per menu section holding the links the menu shows for that user. The cards are comfort, not security.
/// </summary>
public sealed class HomeCompositionTests : ShellTestContext
{
    private IRenderedComponent<Home> RenderHome(string environment = "Development")
    {
        UseShellEnvironment(Breakpoint.Lg, environment: environment);
        return Render<Home>();
    }

    private void SignInWith(params string[] permissions) =>
        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims([.. permissions.Select(permission => new Claim(WebAuthClaims.Permission, permission))]);

    private static string[] Titles(IRenderedComponent<Home> home) =>
        [.. home.FindAll(".app-section-card-title").Select(title => title.TextContent.Trim())];

    private static string[] Links(IRenderedComponent<Home> home) =>
        [.. home.FindAll(".app-section-card a").Select(link => link.GetAttribute("href")!)];

    [Fact]
    public void Visitor_SeesTheWelcomeTheNoticeAndTheTwoWaysIn_AndNoCardAndNoAmberAction()
    {
        var home = RenderHome();

        home.Find("h1").TextContent.Trim().Should().Be("Practice like it is exam day");
        home.Markup.Should().Contain("Realistic practice exams, and a coach that tells you what to study next.");
        home.Find(".app-alert").TextContent.Should().Contain("Simulab is being built");
        home.Find("a[href='/sign-in']").TextContent.Trim().Should().Be("Sign in");
        home.Find("a[href='/sign-up']").TextContent.Trim().Should().Be("Create account");
        home.FindAll(".app-section-card").Should().BeEmpty();
        home.FindAll(".app-action").Should().BeEmpty();
    }

    [Fact]
    public void Visitor_SignInIsThePrimaryActionAndCreateAccountIsSecondary()
    {
        var home = RenderHome();

        home.Find("a[href='/sign-in']").ClassList.Should().Contain("mud-button-filled");
        home.Find("a[href='/sign-up']").ClassList.Should().Contain("mud-button-outlined");
    }

    [Fact]
    public void SignedInWithOnlyTheCatalogPermission_SeesOneStudyCardWithOneLink_AndNoSignInLink()
    {
        SignInWith(CatalogPermissions.Browse);

        var home = RenderHome();

        home.Find("h1").TextContent.Trim().Should().Be("Welcome back");
        Titles(home).Should().Equal("Study");
        home.FindAll(".app-section-card a").Should().ContainSingle();
        var link = home.Find(".app-section-card a[href='/catalog']");
        link.TextContent.Trim().Should().Be("Catalog");
        link.QuerySelector("svg").Should().NotBeNull("the link shows the icon the menu uses");
        home.FindAll("a[href='/sign-in']").Should().BeEmpty();
        home.FindAll("a[href='/sign-up']").Should().BeEmpty();
    }

    [Fact]
    public void SignedInAsAdministrator_SeesTheCardsInMenuOrder_WithTheLinksTheMenuShows()
    {
        SignInWith(
            CatalogPermissions.Browse, CatalogPermissions.Manage, QuestionBankPermissions.Manage,
            QuestionBankPermissions.Review, IdentityPermissions.RolesManage);
        var home = RenderHome();
        var menu = Render<NavMenu>();

        Titles(home).Should().Equal("Study", "Content", "Administration", "Development");
        var menuRoutes = menu.FindAll("a[data-route]").Select(link => link.GetAttribute("data-route")!).Where(route => route != "/").ToArray();
        Links(home).Should().Equal(menuRoutes, "Home and the menu are built from the same registry and cannot disagree");
    }

    [Fact]
    public void SignedInAsAdministratorInProduction_SeesNoDevelopmentLink()
    {
        SignInWith(CatalogPermissions.Browse, CatalogPermissions.Manage, IdentityPermissions.RolesManage);

        var home = RenderHome("Production");

        Links(home).Should().NotContain(route => route.StartsWith("/dev", StringComparison.Ordinal));
        Titles(home).Should().NotContain("Development");
    }

    [Fact]
    public void SignedInWithNoPermission_SeesOnlyTheTitleTheTaglineAndTheNotice()
    {
        SignInWith();

        var home = RenderHome();

        home.Find("h1").TextContent.Trim().Should().Be("Welcome back");
        home.Markup.Should().Contain("Realistic practice exams, and a coach that tells you what to study next.");
        home.Find(".app-alert").TextContent.Should().Contain("Simulab is being built");
        home.FindAll(".app-section-card").Should().BeEmpty();
        home.FindAll("a[href='/sign-in']").Should().BeEmpty();
    }
}
