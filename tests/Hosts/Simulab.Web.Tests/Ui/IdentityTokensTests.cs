using System.Globalization;
using System.Text.Json.Nodes;
using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-63: the identity recorded in <c>docs/design/identity.tokens.json</c> and <c>DESIGN.md</c> is the identity
/// <see cref="SimulabTheme"/> paints. A colour, a font, a size or the radius changed on one side only fails here, by token name.
/// </summary>
public sealed class IdentityTokensTests
{
    private static readonly string Root = FindRoot();

    private static readonly JsonNode Tokens = JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "docs", "design", "identity.tokens.json")))!;

    [Fact]
    public void Colours_EqualTheThemeInBothModes()
    {
        IdentityTokensComparison.CompareColours(SimulabTheme.Create(), Tokens).Should().BeEmpty();
    }

    [Fact]
    public void Colours_AreRecordedForEveryColourTheThemeSets()
    {
        // BR1: the guard against an empty comparison: both sides hold the whole set, not a handful.
        var fromTheme = IdentityTokensComparison.ThemeColours(SimulabTheme.Create());

        fromTheme.Should().HaveCountGreaterThan(30);
        IdentityTokensComparison.TokenColours(Tokens).Keys.Should().BeEquivalentTo(fromTheme.Keys);
    }

    [Fact]
    public void Colours_AThemeColourWithNoToken_FailsByName()
    {
        var theme = SimulabTheme.Create();
        theme.PaletteLight.InfoContrastText = "#123456";

        IdentityTokensComparison.CompareColours(theme, Tokens).Should().ContainSingle()
            .Which.Should().Contain("'infoContrastText'").And.Contain("#123456");
    }

    [Fact]
    public void Colours_AChangedThemeValue_FailsWithTheModeAndBothValues()
    {
        var theme = SimulabTheme.Create();
        theme.PaletteDark.Primary = "#000000";

        IdentityTokensComparison.CompareColours(theme, Tokens).Should().ContainSingle()
            .Which.Should().Contain("'primary'").And.Contain("dark").And.Contain("#000000").And.Contain("#64A2E3");
    }

    [Fact]
    public void Colours_ATokenTheThemeDoesNotSet_FailsByName()
    {
        var theme = SimulabTheme.Create();
        theme.PaletteLight.Success = new PaletteLight().Success;
        theme.PaletteDark.Success = new PaletteDark().Success;

        IdentityTokensComparison.CompareColours(theme, Tokens).Should().Contain(message => message.Contains("token 'success' names no palette colour", StringComparison.Ordinal));
    }

    [Fact]
    public void Typography_AndRadius_EqualTheTheme()
    {
        IdentityTokensComparison.CompareTypography(SimulabTheme.Create(), Tokens).Should().BeEmpty();
    }

    [Fact]
    public void Typography_AChangedSize_FailsByName()
    {
        var theme = SimulabTheme.Create();
        theme.Typography.H1.FontSize = "2rem";
        theme.LayoutProperties.DefaultBorderRadius = "4px";

        IdentityTokensComparison.CompareTypography(theme, Tokens).Should().HaveCount(2)
            .And.Contain(message => message.Contains("'typography.h1.fontSize'", StringComparison.Ordinal) && message.Contains("2rem", StringComparison.Ordinal))
            .And.Contain(message => message.Contains("'shape.borderRadius'", StringComparison.Ordinal) && message.Contains("4px", StringComparison.Ordinal));
    }

    [Fact]
    public void Identity_NamesTheIconFamilyAndTheSource()
    {
        var agile = Tokens["$extensions"]!["agile"]!;

        agile["iconFamily"]!.GetValue<string>().Should().Be("Material Outlined");
        agile["source"]!.GetValue<string>().Should().Be("file");
        agile["sourceRef"]!.GetValue<string>().Should().Be("src/Hosts/Simulab.Web/Theme/SimulabTheme.cs");
        DateOnly.TryParseExact(agile["date"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            .Should().BeTrue("the tokens say the day they were recorded");
    }

    [Fact]
    public void Tokens_FollowTheDtcgShape()
    {
        Tokens["$schema"]!.GetValue<string>().Should().Contain("designtokens.org");
        var group = Tokens["color"]!.AsObject();
        var inherited = group["$type"]?.GetValue<string>();

        foreach (var (name, token) in group.Where(entry => !entry.Key.StartsWith('$')))
        {
            (token!["$type"]?.GetValue<string>() ?? inherited).Should().Be("color", $"token '{name}' is a colour");
            AssertColour(name, "light", token["$value"]);
            AssertColour(name, "dark", token["$extensions"]?["mode"]?["dark"]);
        }
    }

    [Fact]
    public void DesignMd_FrontMatterGivesTheValueOfEveryToken()
    {
        var frontMatter = IdentityTokensComparison.ParseFrontMatter(File.ReadAllText(Path.Combine(Root, "docs", "design", "DESIGN.md")));

        IdentityTokensComparison.CompareFrontMatter(IdentityTokensComparison.ExpectedFrontMatter(Tokens), frontMatter).Should().BeEmpty();
    }

    [Fact]
    public void DesignMd_AValueThatDiffersOrIsMissing_FailsByName()
    {
        var expected = IdentityTokensComparison.ExpectedFrontMatter(Tokens);
        var altered = expected.ToDictionary(entry => entry.Key, entry => entry.Value);
        altered["color.primary.dark"] = "#000000";
        altered.Remove("shape.borderRadius");
        altered["color.invented.light"] = "#FFFFFF";

        IdentityTokensComparison.CompareFrontMatter(expected, altered).Should().HaveCount(3)
            .And.Contain(message => message.Contains("'color.primary.dark'", StringComparison.Ordinal) && message.Contains("#000000", StringComparison.Ordinal))
            .And.Contain(message => message.Contains("no 'shape.borderRadius'", StringComparison.Ordinal))
            .And.Contain(message => message.Contains("'color.invented.light' names no token", StringComparison.Ordinal));
    }

    /// <summary>The colour object, in sRGB, with components that agree with the hex (and the alpha, when there is one).</summary>
    private static void AssertColour(string name, string mode, JsonNode? colour)
    {
        colour.Should().NotBeNull($"token '{name}' has a {mode} value");
        colour!["colorSpace"]!.GetValue<string>().Should().Be("srgb", $"token '{name}' ({mode})");

        var hex = colour["hex"]!.GetValue<string>();
        hex.Length.Should().BeOneOf([7, 9], $"token '{name}' ({mode}) hex is #RRGGBB or #RRGGBBAA");
        var components = colour["components"]!.AsArray().Select(component => component!.GetValue<double>()).ToArray();
        components.Should().HaveCount(3, $"token '{name}' ({mode}) has red, green and blue");

        for (var channel = 0; channel < 3; channel++)
        {
            var expected = int.Parse(hex.AsSpan((channel * 2) + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Math.Round(components[channel] * 255).Should().Be(expected, $"token '{name}' ({mode}) component {channel} agrees with its hex");
        }

        if (hex.Length == 9)
        {
            var expectedAlpha = int.Parse(hex.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Math.Round(colour["alpha"]!.GetValue<double>() * 255).Should().Be(expectedAlpha, $"token '{name}' ({mode}) alpha agrees with its hex");
        }
        else
        {
            colour["alpha"].Should().BeNull($"token '{name}' ({mode}) is opaque");
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx not found above the test output.");
    }
}
