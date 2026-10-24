using System.Security.Claims;
using Bunit;
using MudBlazor;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-134, owner's validation: the Development section (UI kit, AI gateway) is for the administrator only. In the
/// Development environment a student, and a visitor, no longer see it in the menu or on Home; the administrator does.
/// </summary>
public sealed class DevelopmentSectionAdminOnlyTests : ShellTestContext
{
    private void SignInWith(params string[] permissions) =>
        Authorization.SetAuthorized("ana@exemplo.com")
            .SetClaims([.. permissions.Select(permission => new Claim(WebAuthClaims.Permission, permission))]);

    [Fact]
    public void Visible_InDevelopment_HidesTheDevelopmentPagesFromAStudentAndAVisitor()
    {
        var student = NavigationItems.Visible(NavigationItems.All, isDevelopment: true, permission => permission == CatalogPermissions.Browse);
        var visitor = NavigationItems.Visible(NavigationItems.All, isDevelopment: true, _ => false);

        student.Select(item => item.Route).Should().NotContain(["/dev/ui", "/dev/ai"]);
        visitor.Select(item => item.Route).Should().NotContain(["/dev/ui", "/dev/ai"]);
    }

    [Fact]
    public void Visible_InDevelopment_ShowsTheDevelopmentPagesToAnAdministrator()
    {
        var admin = NavigationItems.Visible(NavigationItems.All, isDevelopment: true, permission => permission == IdentityPermissions.RolesManage);

        admin.Select(item => item.Route).Should().Contain(["/dev/ui", "/dev/ai"]);
    }

    [Fact]
    public void Visible_InProduction_HidesTheDevelopmentPagesFromAnAdministratorToo()
    {
        var admin = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, permission => permission == IdentityPermissions.RolesManage);

        admin.Select(item => item.Route).Should().NotContain(["/dev/ui", "/dev/ai"]);
    }

    [Fact]
    public void Home_InDevelopment_ForAStudent_HasNoDevelopmentCard()
    {
        SignInWith(CatalogPermissions.Browse);
        UseShellEnvironment(Breakpoint.Lg, environment: "Development");

        var home = Render<Home>();

        home.FindAll(".app-section-card-title").Select(title => title.TextContent.Trim()).Should().Equal("Study");
        home.FindAll("a[href^='/dev']").Should().BeEmpty();
    }

    [Fact]
    public void Menu_InDevelopment_ForAStudent_HasNoDevelopmentSection()
    {
        SignInWith(CatalogPermissions.Browse);
        UseShellEnvironment(Breakpoint.Lg, environment: "Development");
        Render<MudPopoverProvider>();

        var menu = Render<NavMenu>();

        menu.FindAll("a[data-route^='/dev']").Should().BeEmpty();
        menu.FindAll(".app-nav-section").Select(header => header.TextContent.Trim()).Should().Equal("Study");
    }

    [Fact]
    public void HomeAndMenu_InDevelopment_ForAnAdministrator_ShowTheDevelopmentSection()
    {
        SignInWith(CatalogPermissions.Browse, IdentityPermissions.RolesManage);
        UseShellEnvironment(Breakpoint.Lg, environment: "Development");
        Render<MudPopoverProvider>();

        var home = Render<Home>();
        var menu = Render<NavMenu>();

        home.FindAll(".app-section-card-title").Select(title => title.TextContent.Trim()).Should().Contain("Development");
        home.FindAll("a[href^='/dev']").Should().HaveCount(2);
        menu.FindAll("a[data-route^='/dev']").Should().HaveCount(2);
    }
}
