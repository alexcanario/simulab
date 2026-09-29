using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages.Admin;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-9 BR10, F-14 BR7, F-33 BR4: every back office page is behind a permission. Without it the router shows
/// the ordinary Not Found page (F-6), which is checked on screen; this pins the attribute that causes it,
/// and which permission each page asks for — a page guarded by another module's permission is a hole.
/// </summary>
public sealed class AdminPagesAuthorizationTests
{
    private static readonly Dictionary<string, string> Expected = new(StringComparer.Ordinal)
    {
        ["Roles"] = IdentityPermissions.RolesManage,
        ["Users"] = IdentityPermissions.RolesManage,
        ["RoleHistory"] = IdentityPermissions.RolesManage,
        ["AccountEvents"] = IdentityPermissions.RolesManage,
        ["Organizers"] = CatalogPermissions.Manage,
        ["IssuingAuthorities"] = CatalogPermissions.Manage,
        // F-34: the list and the form page are two routes of the same screen, so both carry the gate.
        ["Exams"] = CatalogPermissions.Manage,
        ["ExamForm"] = CatalogPermissions.Manage,
        // F-35 BR18: the edition page has two routes (adding and editing) and the same gate as the exam page.
        ["ExamEditionForm"] = CatalogPermissions.Manage
    };

    [Fact]
    public void EveryAdminPage_RequiresItsModulesPermission()
    {
        var pages = typeof(RoleHistory).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template.StartsWith("/admin/", StringComparison.Ordinal)))
            .ToList();

        pages.Select(page => page.Name).Should().BeEquivalentTo(Expected.Keys);
        foreach (var page in pages)
        {
            page.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy)
                .Should().Equal([PermissionPolicy.Prefix + Expected[page.Name]], page.Name);
        }
    }

    /// <summary>
    /// F-33, BR2: the Web builds one policy per name in <see cref="WebPermissions.All"/>. A page or a menu
    /// item asking for a permission that is not in that list has no policy to resolve and throws when it is
    /// opened, while the Api still answers - a failure that lands far from its cause.
    /// </summary>
    [Fact]
    public void EveryPermissionAPageOrMenuItemAsksFor_HasAPolicyInTheWeb()
    {
        var asked = Expected.Values
            .Concat(NavigationItems.All.Select(item => item.RequiredPermission).OfType<string>())
            .Distinct(StringComparer.Ordinal);

        asked.Should().BeSubsetOf(WebPermissions.All);
    }
}
