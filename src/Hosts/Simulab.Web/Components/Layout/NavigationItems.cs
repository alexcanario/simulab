using Microsoft.AspNetCore.Components.Routing;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Layout;

/// <summary>The menu registry: the only place that lists menu items.</summary>
public static class NavigationItems
{
    public static readonly IReadOnlyList<NavigationItem> All =
    [
        new(null, "/", AppIcons.Home, "Nav.Home", NavLinkMatch.All),
        new(NavigationSection.Content, "/admin/organizers", AppIcons.Organizers, "Nav.Organizers", RequiredPermission: CatalogPermissions.Manage),
        new(NavigationSection.Administration, "/admin/roles", AppIcons.Roles, "Nav.Roles", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/users", AppIcons.Users, "Nav.Users", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/role-history", AppIcons.History, "Nav.RoleHistory", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/account-events", AppIcons.Security, "Nav.AccountEvents", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Development, "/dev/ui", AppIcons.Build, "Nav.Dev.UiKit", DevelopmentOnly: true),
    ];

    /// <summary>Items the current user may see (F-6, BR6): a permission-gated item needs <paramref name="hasPermission"/> to say yes.</summary>
    public static IReadOnlyList<NavigationItem> Visible(IEnumerable<NavigationItem> items, bool isDevelopment, Func<string, bool> hasPermission) =>
        [.. items.Where(item =>
            (item.RequiredPermission is null || hasPermission(item.RequiredPermission))
            && (isDevelopment || !item.DevelopmentOnly))];

    /// <summary>Visible sectioned items grouped by section, in section order; empty sections are left out.</summary>
    public static IReadOnlyList<IGrouping<NavigationSection, NavigationItem>> Sections(IEnumerable<NavigationItem> visible) =>
        [.. visible
            .Where(item => item.Section is not null)
            .GroupBy(item => item.Section!.Value)
            .OrderBy(group => group.Key)];
}
