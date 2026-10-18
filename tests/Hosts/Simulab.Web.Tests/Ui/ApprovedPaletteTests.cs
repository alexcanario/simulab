using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC1: every role of the palette holds the value of the item's "Approved list" (1. Palette), in both themes.
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
        ("Background", "#F4F6F9", "#0B1B2E"),
        ("Surface", "#FFFFFF", "#12263A"),
        ("BackgroundGray", "#E9EDF2", "#081522"),
        ("ActionDisabledBackground", "#E9EDF2", "#081522"),
        // 4-5. Ink and ink-muted (text secondary and action default).
        ("TextPrimary", "#0B1B2E", "#F4F6F9"),
        ("TextSecondary", "#52647A", "#B8C4D2"),
        ("ActionDefault", "#52647A", "#B8C4D2"),
        // 6-7. Line-strong (inputs) and line (dividers, table lines).
        ("LinesInputs", "#6B7C91", "#6A829A"),
        ("Divider", "#DDE3EA", "#26405A"),
        ("TableLines", "#DDE3EA", "#26405A"),
        ("LinesDefault", "#DDE3EA", "#26405A"),
        // 8. Primary.
        ("Primary", "#064F80", "#2AA8F0"),
        ("PrimaryContrastText", "#FFFFFF", "#0B1B2E"),
        // 9. Info and links.
        ("Info", "#0872B8", "#2AA8F0"),
        ("InfoContrastText", "#FFFFFF", "#0B1B2E"),
        // 10. Secondary and success (green).
        ("Secondary", "#00806B", "#00A68C"),
        ("SecondaryContrastText", "#FFFFFF", "#0B1B2E"),
        ("Success", "#00806B", "#00A68C"),
        ("SuccessContrastText", "#FFFFFF", "#0B1B2E"),
        // 11. Tertiary (the orange action).
        ("Tertiary", "#E8650A", "#FF7A2E"),
        ("TertiaryContrastText", "#0B1B2E", "#0B1B2E"),
        // 12-13. Warning and error.
        ("Warning", "#D98B1A", "#D98B1A"),
        ("WarningContrastText", "#0B1B2E", "#0B1B2E"),
        ("Error", "#BE3737", "#E06C6C"),
        ("ErrorContrastText", "#FFFFFF", "#0B1B2E"),
        // 14. App bar (its subline is the text secondary colour).
        ("AppbarBackground", "#FFFFFF", "#12263A"),
        ("AppbarText", "#0B1B2E", "#F4F6F9"),
        // 15. Sidebar.
        ("DrawerBackground", "#0F3266", "#081522"),
        ("DrawerText", "rgba(255,255,255,0.92)", "#B8C4D2"),
        ("DrawerIcon", "rgba(255,255,255,0.92)", "#B8C4D2"),
        // 17. Tint blue (informative highlights).
        ("PrimaryLighten", "#DCEBF7", "#16344F"),
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

    /// <summary>8, 10, 11: the keycap edge under a filled primary, green and orange button.</summary>
    [Theory]
    [InlineData("light", "#043A5E", "#00574A", "#A84606")]
    [InlineData("dark", "#0F6BA8", "#006B5B", "#B24A0C")]
    public void KeycapEdges_HoldTheApprovedValues(string theme, string primary, string secondary, string tertiary)
    {
        var edges = theme == "light" ? SimulabTheme.LightKeycapEdges : SimulabTheme.DarkKeycapEdges;

        edges.Primary.Should().Be(primary);
        edges.Secondary.Should().Be(secondary);
        edges.Tertiary.Should().Be(tertiary);
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
