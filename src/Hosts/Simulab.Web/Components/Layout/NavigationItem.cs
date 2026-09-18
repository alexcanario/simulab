using Microsoft.AspNetCore.Components.Routing;

namespace Simulab.Web.Components.Layout;

/// <summary>One menu entry. Features add screens to the menu by adding an item to <see cref="NavigationItems"/>.</summary>
/// <param name="Section">Null for top-level items shown before the sections.</param>
/// <param name="Route">Base-relative route, starting with "/".</param>
/// <param name="Icon">An <c>AppIcons</c> constant.</param>
/// <param name="ResourceKey">Key of the label in the shared resources.</param>
/// <param name="Match">Exact for "/", prefix for the rest.</param>
/// <param name="RequiredPermission">Permission name; the item is hidden until permission checks exist (F-6).</param>
/// <param name="DevelopmentOnly">Shown only in the Development environment.</param>
public sealed record NavigationItem(
    NavigationSection? Section,
    string Route,
    string Icon,
    string ResourceKey,
    NavLinkMatch Match = NavLinkMatch.Prefix,
    string? RequiredPermission = null,
    bool DevelopmentOnly = false)
{
    /// <summary>True when <paramref name="path"/> (base-relative, with or without the leading "/") is this item's page.</summary>
    public bool IsActive(string path)
    {
        var current = "/" + path.Split('?', '#')[0].Trim('/');
        var route = "/" + Route.Trim('/');

        if (Match == NavLinkMatch.All || route == "/")
        {
            return string.Equals(current, route, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(current, route, StringComparison.OrdinalIgnoreCase)
            || current.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase);
    }
}
