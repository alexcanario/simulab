using System.Text.RegularExpressions;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC6 (BR6): no diffuse shadow. The theme draws no elevation, and cards, the app bar, the sidebar, menus and
/// dialogs are separated by a 1px border in the line colour. The only shadow left is the keycap edge, which has no blur.
/// </summary>
public sealed partial class SurfaceBorderTests
{
    [Fact]
    public void Theme_DrawsNoElevation()
    {
        SimulabTheme.Create().Shadows.Elevation.Should().OnlyContain(shadow => shadow == "none");
    }

    [Theory]
    [InlineData(".mud-paper:not(.mud-paper-square):not(.mud-popover)", "border")]
    [InlineData(".mud-appbar", "border-bottom")]
    [InlineData(".app-drawer.mud-drawer", "border-right")]
    [InlineData(".mud-popover", "border")]
    [InlineData(".mud-dialog", "border")]
    [InlineData(".app-section-card", "border")]
    [InlineData(".app-form-card", "border")]
    [InlineData(".app-auth-card", "border")]
    public void Surface_IsSeparatedByAOnePixelLine(string selector, string property)
    {
        AppCssColours.Declaration(selector, property).Should().Be("1px solid var(--mud-palette-divider)");
    }

    /// <summary>Every shadow left in a stylesheet is a ring or an edge: no blur, so nothing is diffuse.</summary>
    [Fact]
    public void EveryShadow_HasNoBlur()
    {
        var css = CommentPattern().Replace(File.ReadAllText(AppCss()), string.Empty);
        var diffuse = ShadowPattern().Matches(css)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(value => value != "none" && !value.Split(',').All(HasNoBlur))
            .ToList();

        diffuse.Should().BeEmpty();
        HasNoBlur("0 0 6px 1px red").Should().BeFalse();
        HasNoBlur("inset 0 0 0 2px var(--mud-palette-primary)").Should().BeTrue();
    }

    /// <summary>A shadow's third length is its blur; a ring (0 0 0 2px) and an edge (0 4px 0) have none.</summary>
    private static bool HasNoBlur(string shadow)
    {
        var lengths = shadow.Replace("inset", string.Empty, StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("var(", StringComparison.Ordinal) && !part.EndsWith(')'))
            .ToArray();
        return lengths.Length < 3 || lengths[2] is "0" or "0px";
    }

    private static string AppCss()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web", "wwwroot", "app.css");
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"box-shadow\s*:\s*([^;]+);")]
    private static partial Regex ShadowPattern();
}
