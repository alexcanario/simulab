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

    private static double Contrast(string foreground, string background) => ColourContrast.Ratio(foreground, background);
}
