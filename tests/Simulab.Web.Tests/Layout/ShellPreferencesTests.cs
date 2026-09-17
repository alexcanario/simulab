using Simulab.Web.Components.Layout;

namespace Simulab.Web.Tests.Layout;

public class ShellPreferencesTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("purple", null)]
    [InlineData("dark", true)]
    [InlineData("light", false)]
    public void From_ThemeCookie_ReadsChoice(string? cookie, bool? expected)
    {
        ShellPreferences.From(cookie, null).DarkMode.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("expanded", false)]
    [InlineData("collapsed", true)]
    [InlineData("other", false)]
    public void From_NavigationCookie_ReadsCollapsed(string? cookie, bool expected)
    {
        ShellPreferences.From(null, cookie).NavigationCollapsed.Should().Be(expected);
    }
}
