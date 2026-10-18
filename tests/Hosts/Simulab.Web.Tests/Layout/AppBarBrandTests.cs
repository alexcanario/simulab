using System.Globalization;
using Bunit;
using MudBlazor;
using Simulab.Web.Components.Layout;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-125 AC9 (BR9): from the left, the menu button, the brand symbol (decorative), "Simulab" with "Control Alt Systems"
/// under it, then the theme switch, the language switch and the user menu. The home link keeps its accessible name, both
/// texts come from resource keys, and the symbol follows the theme. The 375px fit is checked by the stylesheet here and
/// on screen in the validation script.
/// </summary>
public sealed class AppBarBrandTests : ShellTestContext
{
    private IRenderedComponent<MainLayout> RenderShell(bool? dark = null)
    {
        UseShellEnvironment();
        var preferences = dark is null ? ShellPreferences.Default : ShellPreferences.Default with { DarkMode = dark };
        return Render<MainLayout>(p => p
            .Add(l => l.Body, ProbeBody)
            .AddCascadingValue(preferences));
    }

    [Fact]
    public void AppBar_ShowsTheBrandBetweenTheMenuAndTheSwitches()
    {
        var bar = RenderShell().Find(".mud-appbar");

        var order = bar.QuerySelectorAll(".app-menu-button, .app-brand-symbol, .app-brand-name, .app-brand-company, .app-theme-switch, .mud-menu, .app-user-menu")
            .Select(element => element.ClassList.First(name => name.StartsWith("app-", StringComparison.Ordinal) || name == "mud-menu"))
            .ToList();

        order.Should().Equal("app-menu-button", "app-brand-symbol", "app-brand-name", "app-brand-company", "app-theme-switch", "mud-menu", "app-user-menu");
    }

    [Fact]
    public void Brand_IsADecorativeSymbolAndTwoLinesInsideTheHomeLink()
    {
        var link = RenderShell().Find(".mud-appbar a.app-name");

        link.GetAttribute("href").Should().Be("/");
        link.GetAttribute("aria-label").Should().Be("Simulab, home page");
        var symbol = link.QuerySelector("img.app-brand-symbol")!;
        symbol.GetAttribute("aria-hidden").Should().Be("true");
        symbol.GetAttribute("alt").Should().BeEmpty();
        link.QuerySelector(".app-brand-name")!.TextContent.Trim().Should().Be("Simulab");
        link.QuerySelector(".app-brand-company")!.TextContent.Trim().Should().Be("Control Alt Systems");
    }

    [Theory]
    [InlineData(false, "brand/keys-symbol.svg")]
    [InlineData(true, "brand/keys-symbol-dark.svg")]
    public void Symbol_FollowsTheTheme(bool dark, string source)
    {
        var shell = RenderShell(dark);

        shell.WaitForAssertion(() => shell.Find("img.app-brand-symbol").GetAttribute("src").Should().Be(source));
    }

    [Theory]
    [InlineData("pt-BR", "Simulab, página inicial")]
    [InlineData("pt-PT", "Simulab, página inicial")]
    public void Brand_TextsComeFromTheResources(string culture, string homeLink)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        var link = RenderShell().Find(".mud-appbar a.app-name");

        link.GetAttribute("aria-label").Should().Be(homeLink);
        link.QuerySelector(".app-brand-company")!.TextContent.Trim().Should().Be("Control Alt Systems");
    }

    /// <summary>At 375px the two lines keep their own line each and never wrap word by word.</summary>
    [Fact]
    public void Brand_NeverWraps()
    {
        AppCssColours.Declaration(".app-brand-text", "white-space").Should().Be("nowrap");
        AppCssColours.Declaration(".app-brand-text", "min-width").Should().Be("0");
        AppCssColours.Declaration(".app-brand-company", "text-overflow").Should().Be("ellipsis");
    }

    [Fact]
    public void Bar_IsTheThemesAppBarColourWithItsLineBelow()
    {
        var bar = RenderShell().Find(".mud-appbar");

        bar.ClassList.Should().NotContain(name => name.StartsWith("mud-elevation-", StringComparison.Ordinal) && name != "mud-elevation-0");
        AppCssColours.Declaration(".mud-appbar", "border-bottom").Should().Be("1px solid var(--mud-palette-divider)");
    }
}
