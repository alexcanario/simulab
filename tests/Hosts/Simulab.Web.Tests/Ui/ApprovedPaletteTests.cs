using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-148 AC1 (replaces F-125 AC1): every role of the palette holds the value of the item's "Approved list" (1. Palette), in both themes.
/// The values are written here once, from the list, so a tone changed in the theme fails by role and theme.
/// <see cref="IdentityTokensTests"/> keeps <c>identity.tokens.json</c> and <c>DESIGN.md</c> equal to the theme.
/// </summary>
public sealed class ApprovedPaletteTests
{
    private static readonly MudTheme Theme = SimulabTheme.Create();

    /// <summary>Palette token, light value, dark value — in the order of the approved list.</summary>
    private static readonly (string Token, string Light, string Dark)[] Approved =
    [
        // 1-3. Page, surface, surface sunken (disabled, wells).
        ("Background", "#F2F5FB", "#0B2149"),
        ("Surface", "#FFFFFF", "#12386F"),
        ("BackgroundGray", "#E6ECF5", "#081938"),
        ("ActionDisabledBackground", "#E6ECF5", "#081938"),
        // 4-5. Ink and ink-muted (text secondary and action default).
        ("TextPrimary", "#0B1B2E", "#F2F5FB"),
        ("TextSecondary", "#52647A", "#B8C4D2"),
        ("ActionDefault", "#52647A", "#B8C4D2"),
        // 6-7. Line-strong (inputs) and line (dividers, table lines).
        ("LinesInputs", "#6B7C91", "#7F9BC0"),
        ("Divider", "#D9E1EE", "#24497F"),
        ("TableLines", "#D9E1EE", "#24497F"),
        ("LinesDefault", "#D9E1EE", "#24497F"),
        // 8. Primary: navy in the light theme, blue in the dark one.
        ("Primary", "#0F3266", "#47A0EB"),
        ("PrimaryContrastText", "#FFFFFF", "#0B1B2E"),
        // 9. Info.
        ("Info", "#2478C5", "#47A0EB"),
        ("InfoContrastText", "#FFFFFF", "#0B1B2E"),
        // 11. Secondary and success (teal).
        ("Secondary", "#1A9E8C", "#1A9E8C"),
        ("SecondaryContrastText", "#0B1B2E", "#0B1B2E"),
        ("Success", "#1A9E8C", "#1A9E8C"),
        ("SuccessContrastText", "#0B1B2E", "#0B1B2E"),
        // 12. Tertiary (the amber action).
        ("Tertiary", "#F5A020", "#F5A020"),
        ("TertiaryContrastText", "#0B1B2E", "#0B1B2E"),
        // 13-14. Warning (kept) and error (the design system's danger).
        ("Warning", "#D98B1A", "#D98B1A"),
        ("WarningContrastText", "#0B1B2E", "#0B1B2E"),
        ("Error", "#B83232", "#FF9C94"),
        ("ErrorContrastText", "#FFFFFF", "#0B1B2E"),
        // 15. App bar (its subline is the text secondary colour).
        ("AppbarBackground", "#FFFFFF", "#12386F"),
        ("AppbarText", "#0B1B2E", "#F2F5FB"),
        // 16. Sidebar.
        ("DrawerBackground", "#0F3266", "#081938"),
        ("DrawerText", "rgba(255,255,255,0.92)", "#B8C4D2"),
        ("DrawerIcon", "rgba(255,255,255,0.92)", "#B8C4D2"),
        // 18. Tint blue (informative highlights).
        ("PrimaryLighten", "#DCE8F5", "#1A4583"),
    ];

    public static TheoryData<string, string, string> Roles()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var (token, light, dark) in Approved)
        {
            data.Add(token, "light", light);
            data.Add(token, "dark", dark);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Roles))]
    public void Role_HoldsTheApprovedValue(string token, string theme, string expected)
    {
        var palette = theme == "light" ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        var actual = typeof(Palette).GetProperty(token)!.GetValue(palette)!.ToString()!;

        IdentityTokensComparison.Hex(actual).Should().Be(IdentityTokensComparison.Hex(expected), $"{token} in the {theme} theme is item {token} of the approved palette");
    }

    /// <summary>8, 11, 12, 14: the keycap edge under a filled primary, teal, amber and error button, and the Alt key's blue edge.</summary>
    [Theory]
    [InlineData("light", "#6B7C91", "#107264", "#B87410", "#17528A", "#842424")]
    [InlineData("dark", "#1F6FB5", "#107264", "#B87410", "#1F6FB5", "#A66560")]
    public void KeycapEdges_HoldTheApprovedValues(string theme, string primary, string secondary, string tertiary, string info, string error)
    {
        var edges = theme == "light" ? SimulabTheme.LightKeycapEdges : SimulabTheme.DarkKeycapEdges;

        edges.Primary.Should().Be(primary);
        edges.Secondary.Should().Be(secondary);
        edges.Tertiary.Should().Be(tertiary);
        edges.Info.Should().Be(info);
        edges.Error.Should().Be(error);
    }

    /// <summary>10, 15: the new link role and the brand name are values of the theme, written to the page as variables.</summary>
    [Fact]
    public void LinkAndBrandName_HoldTheApprovedValues()
    {
        SimulabTheme.LightLink.Should().Be("#1D64A8");
        SimulabTheme.DarkLink.Should().Be("#8CC4F5");
        SimulabTheme.LightBrandName.Should().Be("#0F3266");
        SimulabTheme.DarkBrandName.Should().Be("#F2F5FB");
    }

    /// <summary>16: the focus ring is the info blue, 3px, 2px away from the control.</summary>
    [Fact]
    public void FocusRing_IsTheInfoBlueThreePixelsWideTwoPixelsAway()
    {
        const string focus = ":is(a, button, input, select, textarea, [tabindex], .mud-button-root):focus-visible";

        AppCssColours.Declaration(focus, "outline").Should().Be("3px solid var(--mud-palette-info)");
        AppCssColours.Declaration(focus, "outline-offset").Should().Be("2px");
    }
}
