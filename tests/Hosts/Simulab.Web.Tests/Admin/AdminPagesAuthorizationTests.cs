using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;

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
        ["Organizers"] = CatalogPermissions.Manage
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
}
