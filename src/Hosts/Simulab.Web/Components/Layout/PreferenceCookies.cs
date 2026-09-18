namespace Simulab.Web.Components.Layout;

/// <summary>Names and values of the cookies that keep the shell preferences (1 year).</summary>
public static class PreferenceCookies
{
    public const string Theme = "simulab.theme";
    public const string ThemeLight = "light";
    public const string ThemeDark = "dark";

    public const string Navigation = "simulab.nav";
    public const string NavigationExpanded = "expanded";
    public const string NavigationCollapsed = "collapsed";

    public const int MaxAgeSeconds = 365 * 24 * 60 * 60;
}
