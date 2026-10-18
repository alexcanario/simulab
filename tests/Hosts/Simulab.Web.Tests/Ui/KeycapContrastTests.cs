using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC2 (change note v2): the pairs the identity adds, measured in both themes with the colours the kit stylesheet
/// really paints. Text 4.5:1; non-text 3:1 — the fill of every filled button variant against the page and the surface,
/// the outlined button's border, the focus ring. The keycap edge is decorative and has no minimum.
/// <see cref="ThemePaletteTests"/> keeps measuring every token of the palette.
/// </summary>
public sealed class KeycapContrastTests
{
    private static readonly MudTheme Theme = SimulabTheme.Create();

    private static readonly string[] Themes = ["light", "dark"];

    /// <summary>The three filled variants of the approved list: primary, action and destructive.</summary>
    private static readonly (string Variant, string Selector, string Hover)[] Filled =
    [
        ("primary", ".mud-button-filled.mud-button-filled-primary", "PrimaryDarken"),
        ("action", ".mud-button-filled.mud-button-filled-tertiary", "TertiaryDarken"),
        ("destructive", ".mud-button-filled.mud-button-filled-error", "ErrorDarken"),
    ];

    public static TheoryData<string, string> FilledVariants()
    {
        var data = new TheoryData<string, string>();
        foreach (var theme in Themes)
        {
            foreach (var (variant, _, _) in Filled)
            {
                data.Add(theme, variant);
            }
        }

        return data;
    }

    public static TheoryData<string> BothThemes() => new(Themes);

    [Theory]
    [MemberData(nameof(FilledVariants))]
    public void FilledButton_TextReadsAtAaAtRestAndOnHover(string theme, string variant)
    {
        var palette = PaletteOf(theme);
        var (_, selector, hover) = Filled.Single(entry => entry.Variant == variant);
        var fill = Paint(selector, "background-color", palette);
        var text = Paint(selector, "color", palette);

        Ratio(text, fill).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText, $"{variant} at rest ({theme})");
        Ratio(text, Token(palette, hover)).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText, $"{variant} on hover ({theme})");
    }

    [Theory]
    [MemberData(nameof(FilledVariants))]
    public void FilledButton_FillReadsAtTheNonTextMinimumOnThePageAndTheSurface(string theme, string variant)
    {
        var palette = PaletteOf(theme);
        var fill = Paint(Filled.Single(entry => entry.Variant == variant).Selector, "background-color", palette);

        Ratio(fill, palette.Background.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText, $"{variant} fill on the page ({theme})");
        Ratio(fill, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText, $"{variant} fill on the surface ({theme})");
    }

    /// <summary>The secondary button: ink on the surface fill, inside a 2px line-strong border.</summary>
    [Theory]
    [MemberData(nameof(BothThemes))]
    public void OutlinedButton_TextAndBorderRead(string theme)
    {
        var palette = PaletteOf(theme);
        const string selector = ".mud-button-root.mud-button-outlined";
        var fill = Paint(selector, "background-color", palette);
        var border = AppCssColours.Declaration(selector, "border");

        border.Should().StartWith("2px solid ");
        Ratio(Paint(selector, "color", palette), fill).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
        var line = AppCssColours.Resolve(border["2px solid ".Length..], palette);
        Ratio(line, palette.Background.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText);
        Ratio(line, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText);
    }

    /// <summary>The text button is a link: info blue, underlined, readable on the page and the surface.</summary>
    [Theory]
    [MemberData(nameof(BothThemes))]
    public void TextButton_ReadsAsALinkOnThePageAndTheSurface(string theme)
    {
        var palette = PaletteOf(theme);
        const string selector = ".mud-button-root.mud-button-text";
        var text = Paint(selector, "color", palette);

        AppCssColours.Declaration(selector, "text-decoration").Should().StartWith("underline");
        Ratio(text, palette.Background.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
        Ratio(text, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
    }

    /// <summary>The focus ring on the page and the surface, and the drawer's own ring on the navy.</summary>
    [Theory]
    [MemberData(nameof(BothThemes))]
    public void FocusRing_ReadsAtTheNonTextMinimumWhereverItIsDrawn(string theme)
    {
        var palette = PaletteOf(theme);
        var outline = AppCssColours.Declaration(":is(a, button, input, select, textarea, [tabindex], .mud-button-root):focus-visible", "outline");
        var ring = AppCssColours.Resolve(outline["3px solid ".Length..], palette);
        var drawerRing = AppCssColours.Resolve(AppCssColours.Declaration(".app-drawer :focus-visible", "outline-color"), palette);

        Ratio(ring, palette.Background.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText);
        Ratio(ring, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText);
        Ratio(drawerRing, palette.DrawerBackground.ToString()).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText);
    }

    /// <summary>The two lines of the brand in the app bar: the name in the bar's text, the company in the subline.</summary>
    [Theory]
    [MemberData(nameof(BothThemes))]
    public void AppBarBrand_ReadsAtAaOnTheBar(string theme)
    {
        var palette = PaletteOf(theme);
        var bar = palette.AppbarBackground.ToString();

        Ratio(palette.AppbarText.ToString(), bar).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
        Ratio(Paint(".app-brand-company", "color", palette), bar).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
    }

    /// <summary>Text in a sunken well (the disabled fill, a well of the gallery) reads on it.</summary>
    [Theory]
    [MemberData(nameof(BothThemes))]
    public void SunkenSurface_CarriesBothTextColours(string theme)
    {
        var palette = PaletteOf(theme);
        var sunken = palette.BackgroundGray.ToString();

        Ratio(palette.TextPrimary.ToString(), sunken).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
        Ratio(palette.TextSecondary.ToString(), sunken).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText);
    }

    private static Palette PaletteOf(string theme) => theme == "light" ? Theme.PaletteLight : Theme.PaletteDark;

    private static string Paint(string selector, string property, Palette palette) =>
        AppCssColours.Resolve(AppCssColours.Declaration(selector, property), palette);

    private static string Token(Palette palette, string token) =>
        typeof(Palette).GetProperty(token)!.GetValue(palette)!.ToString()!;

    private static double Ratio(string foreground, string background) => ColourContrast.Ratio(foreground, background);
}
