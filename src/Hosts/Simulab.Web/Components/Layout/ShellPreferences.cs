namespace Simulab.Web.Components.Layout;

/// <summary>Shell preferences read from the cookies at the first request, so the page renders right without a flash.</summary>
/// <param name="DarkMode">Null when the user has not chosen: the system preference applies.</param>
/// <param name="NavigationCollapsed">Desktop menu collapsed to icons.</param>
public sealed record ShellPreferences(bool? DarkMode, bool NavigationCollapsed)
{
    public static readonly ShellPreferences Default = new(null, false);

    public static ShellPreferences From(string? themeCookie, string? navigationCookie) => new(
        themeCookie switch
        {
            PreferenceCookies.ThemeDark => true,
            PreferenceCookies.ThemeLight => false,
            _ => null
        },
        navigationCookie == PreferenceCookies.NavigationCollapsed);
}
