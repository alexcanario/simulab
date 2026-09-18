using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Layout;

public class MainLayoutTests : ShellTestContext
{
    private IRenderedComponent<MainLayout> RenderShell(
        Breakpoint breakpoint = Breakpoint.Lg,
        ShellPreferences? preferences = null,
        bool systemDark = false)
    {
        UseShellEnvironment(breakpoint, systemDark);
        return Render<MainLayout>(p => p
            .Add(l => l.Body, ProbeBody)
            .AddCascadingValue(preferences ?? ShellPreferences.Default));
    }

    private static string? MenuLabel(IRenderedComponent<MainLayout> shell) =>
        shell.Find(".app-menu-button").GetAttribute("aria-label");

    private static bool DrawerOpen(IRenderedComponent<MainLayout> shell) =>
        shell.Find(".app-drawer").ClassList.Contains("mud-drawer--open");

    private string IconSvg(string icon) =>
        Render<MudIcon>(p => p.Add(i => i.Icon, icon)).Find("svg").InnerHtml;

    [Fact]
    public void Render_AppBar_HasControlsInOrder()
    {
        var shell = RenderShell();

        var bar = shell.Find(".mud-appbar");
        var controls = bar.QuerySelectorAll(".app-menu-button, .app-name, .app-theme-switch, .mud-menu, .app-user-menu")
            .Select(e => e.ClassList.Contains("app-menu-button") ? "menu"
                : e.ClassList.Contains("app-name") ? "name"
                : e.ClassList.Contains("app-theme-switch") ? "theme"
                : e.ClassList.Contains("app-user-menu") ? "user"
                : "language")
            .ToList();
        controls.Should().Equal("menu", "name", "theme", "language", "user");
        var name = bar.QuerySelector(".app-name")!;
        name.GetAttribute("href").Should().Be("/");
        name.TextContent.Trim().Should().Be("Simulab");
        name.GetAttribute("aria-label").Should().Be("Simulab, home page");
        bar.QuerySelector(".app-user-menu")!.ChildNodes.Should().BeEmpty();
    }

    [Fact]
    public void Render_Desktop_MenuExpandedByDefault()
    {
        var shell = RenderShell();

        shell.WaitForAssertion(() => MenuLabel(shell).Should().Be("Collapse menu"));
        DrawerOpen(shell).Should().BeTrue();
        shell.Find(".app-menu-button").GetAttribute("aria-expanded").Should().Be("true");
        shell.FindAll(".app-nav-section").Should().ContainSingle();
    }

    [Fact]
    public void MenuButton_Desktop_TogglesCollapsedAndWritesCookie()
    {
        var shell = RenderShell();

        shell.Find(".app-menu-button").Click();

        DrawerOpen(shell).Should().BeFalse();
        MenuLabel(shell).Should().Be("Expand menu");
        shell.Find(".app-menu-button").GetAttribute("aria-expanded").Should().Be("false");
        shell.FindAll(".app-nav-section").Should().BeEmpty();
        shell.FindAll(".app-nav-divider").Should().ContainSingle();

        shell.Find(".app-menu-button").Click();

        DrawerOpen(shell).Should().BeTrue();
        MenuLabel(shell).Should().Be("Collapse menu");
        CookieWrites(PreferenceCookies.Navigation).Should().Equal("collapsed", "expanded");
    }

    [Fact]
    public void Render_CollapsedCookie_MenuRendersCollapsed()
    {
        var shell = RenderShell(preferences: new ShellPreferences(null, NavigationCollapsed: true));

        DrawerOpen(shell).Should().BeFalse();
        MenuLabel(shell).Should().Be("Expand menu");
        shell.Find("a[data-route='/']").GetAttribute("aria-label").Should().Be("Home");
    }

    [Fact]
    public void Render_Phone_MenuClosedAndClosesAfterNavigating()
    {
        var shell = RenderShell(Breakpoint.Sm);

        shell.WaitForAssertion(() => MenuLabel(shell).Should().Be("Open menu"));
        DrawerOpen(shell).Should().BeFalse();

        shell.Find(".app-menu-button").Click();
        DrawerOpen(shell).Should().BeTrue();
        MenuLabel(shell).Should().Be("Close menu");
        shell.FindAll(".app-nav-section").Should().ContainSingle("the phone menu shows labels and headers");

        shell.Find("a[data-route='/dev/ui']").GetAttribute("href").Should().Be("/dev/ui");
        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>().NavigateTo("/dev/ui");

        shell.WaitForAssertion(() => DrawerOpen(shell).Should().BeFalse());
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == PreferenceWriter.SetFunction, "the phone menu state is not stored");
    }

    [Fact]
    public void Render_DarkCookie_RendersDarkFromTheStart()
    {
        var shell = RenderShell(preferences: new ShellPreferences(DarkMode: true, false));

        Services.GetRequiredService<ThemeState>().IsDarkMode.Should().BeTrue();
        shell.Find(".app-theme-switch").GetAttribute("aria-label").Should().Be("Switch to light mode");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_NoThemeCookie_FollowsSystemPreference(bool systemDark)
    {
        var shell = RenderShell(systemDark: systemDark);

        shell.WaitForAssertion(() => Services.GetRequiredService<ThemeState>().IsDarkMode.Should().Be(systemDark));
    }

    [Fact]
    public void Render_LightCookie_IgnoresDarkSystemPreference()
    {
        var shell = RenderShell(preferences: new ShellPreferences(DarkMode: false, false), systemDark: true);

        shell.WaitForAssertion(() => MenuLabel(shell).Should().Be("Collapse menu"));
        Services.GetRequiredService<ThemeState>().IsDarkMode.Should().BeFalse();
    }

    [Fact]
    public void ThemeSwitch_Clicked_ChangesThemeAndWritesCookie()
    {
        var shell = RenderShell();
        var theme = Services.GetRequiredService<ThemeState>();

        shell.Find(".app-theme-switch").Click();

        theme.IsDarkMode.Should().BeTrue();
        CookieWrites(PreferenceCookies.Theme).Should().Equal("dark");

        shell.Find(".app-theme-switch").Click();

        theme.IsDarkMode.Should().BeFalse();
        CookieWrites(PreferenceCookies.Theme).Should().Equal("dark", "light");
    }

    [Fact]
    public void ThemeSwitch_EachMode_ShowsTheOtherModeIconAndName()
    {
        var shell = RenderShell();
        var moon = IconSvg(AppIcons.DarkMode);
        var sun = IconSvg(AppIcons.LightMode);

        shell.Find(".app-theme-switch").GetAttribute("aria-label").Should().Be("Switch to dark mode");
        shell.Find(".app-theme-switch svg").InnerHtml.Should().Be(moon);

        shell.Find(".app-theme-switch").Click();

        shell.Find(".app-theme-switch").GetAttribute("aria-label").Should().Be("Switch to light mode");
        shell.Find(".app-theme-switch svg").InnerHtml.Should().Be(sun);
    }

    [Fact]
    public void Render_SkipLink_IsFirstFocusableAndTargetsMain()
    {
        var shell = RenderShell();

        var first = shell.FindAll("a[href], button, input, select, textarea, [tabindex]:not([tabindex='-1'])")[0];
        first.ClassList.Should().Contain("app-skip-link");
        first.GetAttribute("href").Should().Be("#main-content");
        first.TextContent.Should().Be("Skip to main content");
        var main = shell.Find("main#main-content");
        main.GetAttribute("tabindex").Should().Be("-1");
        main.TextContent.Should().Contain("Probe page");
    }

    [Fact]
    public void Render_Drawer_IsLabelledNavigation()
    {
        var shell = RenderShell();

        var drawer = shell.Find(".app-drawer");
        drawer.GetAttribute("role").Should().Be("navigation");
        drawer.GetAttribute("aria-label").Should().Be("Main menu");
    }
}
