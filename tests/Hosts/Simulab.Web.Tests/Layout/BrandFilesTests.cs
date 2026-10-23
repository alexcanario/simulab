using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-148 AC9 (BR9): the brand files are the v2 logos of the design system. The symbol's keys are teal, blue and amber; the
/// dark symbol takes the dark theme's blue; the app icon is the navy tile with the amber key. No file carries the
/// F-125 orange or the old greens and blues.
/// </summary>
public sealed partial class BrandFilesTests
{
    private static readonly string[] OldColours = ["#E8650A", "#FF7A2E", "#A84606", "#B24A0C", "#00806B", "#00A68C", "#00574A", "#006B5B", "#0872B8", "#2AA8F0", "#064F80", "#0F6BA8"];

    [Fact]
    public void Symbol_HasTheTealBlueAndAmberKeys()
    {
        var symbol = Read("keys-symbol.svg");

        Fills(symbol).Should().Contain(["#1A9E8C", "#2478C5", "#F5A020"], "Ctrl is teal, Alt is blue and the restart key is amber");
        Fills(symbol).Should().Contain(["#107264", "#17528A", "#B87410"], "each key has its edge under it");
    }

    [Fact]
    public void DarkSymbol_TakesTheDarkThemesBlueAndKeepsTealAndAmber()
    {
        var symbol = Read("keys-symbol-dark.svg");

        Fills(symbol).Should().Contain(["#1A9E8C", "#47A0EB", "#F5A020"]);
        Fills(symbol).Should().NotContain("#2478C5", "the dark symbol's Alt key is the dark theme's blue");
    }

    [Fact]
    public void AppIcon_IsTheNavyTileWithTheAmberKey()
    {
        var icon = Read("app-icon.svg");

        Fills(icon).Should().Contain(["#0F3266", "#F5A020", "#B87410", "#0B1B2E"]);
        icon.Length.Should().BeLessThan(4096, "the v2 icon is the design system's own, without the embedded manifest of the F-125 copy");
    }

    [Theory]
    [InlineData("keys-symbol.svg")]
    [InlineData("keys-symbol-dark.svg")]
    [InlineData("app-icon.svg")]
    public void BrandFile_HoldsNoColourOfTheOldPalette(string file)
    {
        var content = Read(file).ToUpperInvariant();

        OldColours.Where(colour => content.Contains(colour, StringComparison.Ordinal)).Should().BeEmpty();
        content.Should().NotContain("C2PA", "the embedded content-credentials manifest is not part of the brand file");
    }

    [Theory]
    [InlineData("keys-symbol.svg")]
    [InlineData("keys-symbol-dark.svg")]
    [InlineData("app-icon.svg")]
    public void BrandFile_IsAnSvgWithATitle(string file)
    {
        var content = Read(file);

        content.Should().StartWith("<svg").And.Contain("<title>");
    }

    private static List<string> Fills(string svg) =>
        FillPattern().Matches(svg).Select(match => match.Groups[1].Value.ToUpperInvariant()).Distinct().ToList();

    private static string Read(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return File.ReadAllText(Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web", "wwwroot", "brand", file));
    }

    [GeneratedRegex(@"(?:fill|stroke)=""(#[0-9A-Fa-f]{6})""")]
    private static partial Regex FillPattern();
}
