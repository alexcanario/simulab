using System.Security.Claims;
using Bunit;
using MudBlazor;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Ui;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Layout;

public class NavMenuTests : ShellTestContext
{
    private IRenderedComponent<NavMenu> RenderMenu(
        string environment = "Development",
        string url = "/",
        IReadOnlyList<NavigationItem>? items = null)
    {
        UseShellEnvironment(Breakpoint.Lg, environment: environment, url: url);
        Render<MudPopoverProvider>();
        return Render<NavMenu>(p =>
        {
            if (items is not null)
            {
                p.Add(m => m.Items, items);
            }
        });
    }

    private static List<string> Labels(IRenderedComponent<NavMenu> menu) =>
        [.. menu.FindAll(".mud-nav-link").Select(l => l.TextContent.Trim())];

    private static List<string> Headers(IRenderedComponent<NavMenu> menu) =>
        [.. menu.FindAll(".app-nav-section").Select(h => h.TextContent.Trim())];

    [Fact]
    public void Render_Development_ShowsHomeAndDevelopmentSection()
    {
        var menu = RenderMenu();

        Labels(menu).Should().Equal("Home", "UI kit", "AI gateway");
        Headers(menu).Should().Equal("Development");
    }

    [Fact]
    public void Render_Production_ShowsOnlyHome()
    {
        var menu = RenderMenu(environment: "Production");

        Labels(menu).Should().Equal("Home");
        Headers(menu).Should().BeEmpty();
    }

    [Fact]
    public void Render_EmptySectionAndPermissionItem_AreNotRendered()
    {
        var items = new[]
        {
            new NavigationItem(null, "/", AppIcons.Home, "Nav.Home", Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All),
            new NavigationItem(NavigationSection.Administration, "/admin/roles", AppIcons.Edit, "Nav.Dev.UiKit", RequiredPermission: "roles.manage"),
            new NavigationItem(NavigationSection.Study, "/study", AppIcons.Edit, "Nav.Dev.UiKit"),
        };

        var menu = RenderMenu(items: items);

        Headers(menu).Should().Equal("Study");
        menu.FindAll("a[data-route='/admin/roles']").Should().BeEmpty();
        menu.FindAll("a[data-route='/study']").Should().ContainSingle();
    }

    [Fact]
    public void Render_PermissionItem_IsShownOnceTheVisitorHasTheClaim()
    {
        var items = new[]
        {
            new NavigationItem(null, "/", AppIcons.Home, "Nav.Home", Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All),
            new NavigationItem(NavigationSection.Administration, "/admin/roles", AppIcons.Roles, "Nav.Roles", RequiredPermission: "identity.roles.manage"),
        };
        Authorization.SetAuthorized("admin@exemplo.com").SetClaims(new Claim(WebAuthClaims.Permission, "identity.roles.manage"));

        var menu = RenderMenu(items: items);

        menu.FindAll("a[data-route='/admin/roles']").Should().ContainSingle();
    }

    [Fact]
    public void Render_OnUiKit_OnlyUiKitIsCurrent()
    {
        var menu = RenderMenu(url: "/dev/ui");

        menu.Find("a[data-route='/dev/ui']").GetAttribute("aria-current").Should().Be("page");
        menu.Find("a[data-route='/dev/ui']").ClassList.Should().Contain("active");
        menu.Find("a[data-route='/']").HasAttribute("aria-current").Should().BeFalse();
        menu.Find("a[data-route='/']").ClassList.Should().NotContain("active");
    }

    [Fact]
    public void Render_OnHome_OnlyHomeIsCurrent()
    {
        var menu = RenderMenu(url: "/");

        menu.Find("a[data-route='/']").GetAttribute("aria-current").Should().Be("page");
        menu.Find("a[data-route='/dev/ui']").HasAttribute("aria-current").Should().BeFalse();
    }

    [Fact]
    public void Render_InPortuguese_UsesResourceTexts()
    {
        System.Globalization.CultureInfo.CurrentUICulture = new("pt-BR");

        var menu = RenderMenu();

        Labels(menu).Should().Equal("Início", "Kit de interface", "Gateway de IA");
        Headers(menu).Should().Equal("Desenvolvimento");
    }
}
