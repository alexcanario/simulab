using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// WCAG 2.2 AA (ADR-0001 #29) on the colours a screen shows as text. Found in F-10: the dark palette had
/// kept the light red, so every error message on a dark card sat at 2.93:1. A number, not an opinion.
/// These are the pairs each fix named; <see cref="ThemePaletteTests"/> holds every token of both palettes (F-17).
/// </summary>
public sealed class ThemeContrastTests
{
    private const double MinimumForText = ColourContrast.MinimumForText;

    private const double MinimumForNonText = ColourContrast.MinimumForNonText;

    private static readonly MudTheme Theme = SimulabTheme.Create();

    public static TheoryData<string, string, string> ErrorTextOnItsSurfaces()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light on surface", light.Error.ToString(), light.Surface.ToString() },
            { "light on background", light.Error.ToString(), light.Background.ToString() },
            { "dark on surface", dark.Error.ToString(), dark.Surface.ToString() },
            { "dark on background", dark.Error.ToString(), dark.Background.ToString() }
        };
    }

    /// <summary>The error colour is the text of a field error, a danger zone and an outlined destructive button.</summary>
    [Theory]
    [MemberData(nameof(ErrorTextOnItsSurfaces))]
    public void ErrorText_ReadsAtAaOnItsSurface(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForText, $"error text must be AA readable ({what})");
    }

    /// <summary>And the filled destructive button puts its contrast text on top of that same colour.</summary>
    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void ErrorContrastText_ReadsAtAaOnTheErrorColour(string palette)
    {
        var colours = palette == "light" ? (Theme.PaletteLight.Error, Theme.PaletteLight.ErrorContrastText) : (Theme.PaletteDark.Error, Theme.PaletteDark.ErrorContrastText);

        Contrast(colours.Item2.ToString(), colours.Item1.ToString()).Should().BeGreaterThanOrEqualTo(MinimumForText);
    }

    public static TheoryData<string, string, string> PrimaryTextOnItsSurfaces()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light on surface", light.Primary.ToString(), light.Surface.ToString() },
            { "light on background", light.Primary.ToString(), light.Background.ToString() },
            { "dark on surface", dark.Primary.ToString(), dark.Surface.ToString() },
            { "dark on background", dark.Primary.ToString(), dark.Background.ToString() }
        };
    }

    /// <summary>
    /// B-8: the primary colour is the text of an outlined or text button, the kit's "Try again" and the app name
    /// in the auth bar. At 3.53:1 on the dark card it had been patched away site by site (F-7, B-6, F-9, F-11).
    /// </summary>
    [Theory]
    [MemberData(nameof(PrimaryTextOnItsSurfaces))]
    public void PrimaryText_ReadsAtAaOnItsSurface(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForText, $"primary text must be AA readable ({what})");
    }

    public static TheoryData<string, string, string> PrimaryContrastTextOnItsFills()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light at rest", light.PrimaryContrastText.ToString(), light.Primary.ToString() },
            { "light on hover", light.PrimaryContrastText.ToString(), light.PrimaryDarken },
            { "dark at rest", dark.PrimaryContrastText.ToString(), dark.Primary.ToString() },
            { "dark on hover", dark.PrimaryContrastText.ToString(), dark.PrimaryDarken }
        };
    }

    /// <summary>And a filled primary button puts its contrast text on the primary colour, and on its darken tone on hover.</summary>
    [Theory]
    [MemberData(nameof(PrimaryContrastTextOnItsFills))]
    public void PrimaryContrastText_ReadsAtAaOnThePrimaryColour(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForText, $"filled primary button text must be AA readable ({what})");
    }

    public static TheoryData<string, string, string> AlertContrastTextOnItsSeverities()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light info", light.InfoContrastText.ToString(), light.Info.ToString() },
            { "light success", light.SuccessContrastText.ToString(), light.Success.ToString() },
            { "light warning", light.WarningContrastText.ToString(), light.Warning.ToString() },
            { "dark info", dark.InfoContrastText.ToString(), dark.Info.ToString() },
            { "dark success", dark.SuccessContrastText.ToString(), dark.Success.ToString() },
            { "dark warning", dark.WarningContrastText.ToString(), dark.Warning.ToString() }
        };
    }

    /// <summary>
    /// B-9: the kit's alert (<c>AppAlert</c>) is always filled, so every notice on a page paints its text in the
    /// severity's contrast text on top of the severity colour. Success read at 2.24:1 in the light theme and
    /// warning at 2.74:1 in the dark one, on sign-in, sign-up, account, password and security pages.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlertContrastTextOnItsSeverities))]
    public void AlertContrastText_ReadsAtAaOnItsSeverityColour(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForText, $"a filled alert must be AA readable ({what})");
    }

    public static TheoryData<string, string, string> ActionIconOnItsSurfaces()
    {
        var light = Theme.PaletteLight;
        var dark = Theme.PaletteDark;
        return new TheoryData<string, string, string>
        {
            { "light on surface", light.ActionDefault.ToString(), light.Surface.ToString() },
            { "light on background", light.ActionDefault.ToString(), light.Background.ToString() },
            { "dark on surface", dark.ActionDefault.ToString(), dark.Surface.ToString() },
            { "dark on background", dark.ActionDefault.ToString(), dark.Background.ToString() }
        };
    }

    /// <summary>
    /// B-10: MudBlazor paints an icon-only button and a menu item's icon in <c>ActionDefault</c>: the row actions,
    /// the "More" menu, the filter chip's remove button and the user menu. The icon is the only sign of the
    /// control, so it needs the non-text minimum; it read at 1.94:1 on the dark card.
    /// </summary>
    [Theory]
    [MemberData(nameof(ActionIconOnItsSurfaces))]
    public void ActionIcon_ReadsAtNonTextMinimumOnItsSurfaces(string what, string foreground, string background)
    {
        Contrast(foreground, background).Should().BeGreaterThanOrEqualTo(MinimumForNonText, $"an action icon must be visible ({what})");
    }

    public static TheoryData<string, bool> BothPalettes() => new() { { "light", true }, { "dark", false } };

    /// <summary>
    /// B-16: the hint under every kit field is painted by <c>.app-field-hint</c>, and the colour it names is read
    /// from the stylesheet instead of repeated here — the bug was that nobody had measured the token the CSS
    /// chose. It sat at 1.90:1 on the dark card and 2.64:1 on the light one, against the 4.5:1 of ADR-0001 #29.
    /// </summary>
    [Theory]
    [MemberData(nameof(BothPalettes))]
    public void FieldHint_ReadsAtAaOnItsSurfaces(string what, bool light)
    {
        var palette = light ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        var hint = AppCssColours.Resolve(AppCssColours.Declaration(".app-field-hint", "color"), palette);

        Contrast(hint, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(MinimumForText, $"a field hint must be AA readable on the card ({what})");
        Contrast(hint, palette.Background.ToString()).Should().BeGreaterThanOrEqualTo(MinimumForText, $"a field hint must be AA readable on the page ({what})");
    }

    /// <summary>
    /// B-17: the selected navigation item lightens its background and keeps the drawer's text colour, so the entry
    /// the user is looking at read worse than the ones around it — 4.37:1 in dark. Both colours come from the
    /// stylesheet, and the translucent background is composited over the drawer first.
    /// </summary>
    [Theory]
    [MemberData(nameof(BothPalettes))]
    public void SelectedNavigationItem_ReadsAtAaOnItsOwnBackground(string what, bool light)
    {
        var palette = light ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        const string selected = ".app-drawer .mud-navmenu .mud-nav-link[aria-current=\"page\"]";
        var drawer = palette.DrawerBackground.ToString();
        var surface = AppCssColours.Over(AppCssColours.Declaration(selected, "background-color"), drawer);
        var text = AppCssColours.Resolve(
            AppCssColours.Declaration(selected, "color"),
            palette,
            AppCssColours.Resolve("var(--mud-palette-drawer-text)", palette));

        Contrast(text, surface).Should().BeGreaterThanOrEqualTo(MinimumForText, $"the selected menu item must be AA readable on its own background ({what})");
    }

    /// <summary>
    /// F-43: the status chip's outline is what makes it a pill instead of loose text, so it needs the 3:1 of a
    /// UI boundary. Found on screen: with the divider colour it read 1.42:1 on the dark card and disappeared.
    /// </summary>
    [Theory]
    [MemberData(nameof(BothPalettes))]
    public void StatusChipBorder_ReadsAtTheNonTextMinimum(string what, bool light)
    {
        var palette = light ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        var border = AppCssColours.Resolve(AppCssColours.Declaration(".app-status-chip", "border"), palette);

        Contrast(border, palette.Surface.ToString()).Should().BeGreaterThanOrEqualTo(MinimumForNonText, $"a chip must read as a pill ({what})");
    }

    /// <summary>
    /// B-18: a selected radio card paints its own background (<c>primary-lighten</c>) and the description keeps the
    /// secondary text colour, a pair the F-43 sweep found held by no test. Nothing was wrong on screen — 6.19:1 light
    /// and 4.61:1 dark, the dark one 0.11 above the minimum — so what this pins is the margin: a tone change to
    /// either token now fails the build instead of reaching the screen, which is how B-16 and B-17 got through.
    /// Both colours are read from the stylesheet, never repeated here.
    /// </summary>
    [Theory]
    [MemberData(nameof(BothPalettes))]
    public void SelectedRadioCardDescription_ReadsAtAaOnItsOwnBackground(string what, bool light)
    {
        var palette = light ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        var surface = AppCssColours.Resolve(
            AppCssColours.Declaration(".app-radio-card-selected", "background-color"),
            palette);
        var description = AppCssColours.Resolve(
            AppCssColours.Declaration(".app-radio-card-description", "color"),
            palette);

        Contrast(description, surface).Should().BeGreaterThanOrEqualTo(
            MinimumForText,
            $"the description of a selected radio card must be AA readable on the background that card paints ({what})");
    }

    private static double Contrast(string foreground, string background) => ColourContrast.Ratio(foreground, background);
}
