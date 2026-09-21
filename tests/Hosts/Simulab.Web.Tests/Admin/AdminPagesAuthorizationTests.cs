using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-9 BR10, F-14 BR7: every back office page is behind <c>identity.roles.manage</c>. Without it the router shows
/// the ordinary Not Found page (F-6), which is checked on screen; this pins the attribute that causes it.
/// </summary>
public sealed class AdminPagesAuthorizationTests
{
    [Fact]
    public void EveryAdminPage_RequiresRolesManage()
    {
        var pages = typeof(RoleHistory).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template.StartsWith("/admin/", StringComparison.Ordinal)))
            .ToList();

        pages.Select(page => page.Name).Should().BeEquivalentTo("Roles", "Users", "RoleHistory");
        pages.Should().OnlyContain(page => page.GetCustomAttributes<AuthorizeAttribute>()
            .Any(attribute => attribute.Policy == PermissionPolicy.Prefix + IdentityPermissions.RolesManage));
    }
}
