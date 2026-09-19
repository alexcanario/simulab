using Microsoft.Extensions.Localization;
using Simulab.Identity.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Pages.Admin;

/// <summary>
/// The on-screen text of roles and permissions (F-9, BR12): a system role and a permission are translated by
/// resource key; a custom role's name is data and shows as stored.
/// </summary>
public static class RoleText
{
    public static string NameOf(IStringLocalizer<SharedResources> l, string name, bool isSystem)
    {
        ArgumentNullException.ThrowIfNull(l);
        if (!isSystem)
        {
            return name;
        }

        var text = l[$"Role.System.{name}"];
        return text.ResourceNotFound ? name : text;
    }

    public static string PermissionName(IStringLocalizer<SharedResources> l, string permission) =>
        Translated(l, $"Permission.{permission}.Name", permission);

    public static string PermissionDescription(IStringLocalizer<SharedResources> l, string permission) =>
        Translated(l, $"Permission.{permission}.Description", string.Empty);

    public static string GroupName(IStringLocalizer<SharedResources> l, string group) =>
        Translated(l, $"PermissionGroup.{group}", group);

    private static string Translated(IStringLocalizer<SharedResources> l, string key, string fallback)
    {
        ArgumentNullException.ThrowIfNull(l);
        var text = l[key];
        return text.ResourceNotFound ? fallback : text;
    }

    /// <summary>The catalog grouped by module, in catalog order.</summary>
    public static IReadOnlyList<IGrouping<string, string>> Groups(IEnumerable<string> permissions) =>
        [.. permissions.GroupBy(IdentityPermissions.GroupOf)];
}
