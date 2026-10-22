using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>
/// The Control Alt Systems identity (F-125, ADR-0006): the palette of the item's approved list, Bricolage Grotesque and
/// JetBrains Mono, keycap buttons, borders instead of shadows. ApprovedPaletteTests holds every value; ThemePaletteTests
/// and KeycapContrastTests hold every pair; IdentityTokensTests keeps docs/design in step.
/// </summary>
public static class SimulabTheme
{
    /// <summary>The edge under each filled keycap, light theme. Error is derived (error x0.72): the list gives none.</summary>
    public static KeycapEdges LightKeycapEdges { get; } = new(Primary: "#043A5E", Secondary: "#00574A", Tertiary: "#A84606", Info: "#043A5E", Error: "#892828");

    /// <summary>The same in the dark theme, error x0.65. The edges are decorative (change note v2).</summary>
    public static KeycapEdges DarkKeycapEdges { get; } = new(Primary: "#0F6BA8", Secondary: "#006B5B", Tertiary: "#B24A0C", Info: "#0F6BA8", Error: "#924646");

    public static string LightLink => string.Empty;

    public static string DarkLink => string.Empty;

    public static string LightBrandName => string.Empty;

    public static string DarkBrandName => string.Empty;

    private const string Ink = "#0B1B2E";

    private const string InkMuted = "#52647A";

    private const string Line = "#DDE3EA";

    private const string Sunken = "#E9EDF2";

    private const string DarkPage = "#0B1B2E";

    private const string DarkRaised = "#12263A";

    private const string DarkInk = "#F4F6F9";

    private const string DarkInkMuted = "#B8C4D2";

    private const string DarkLine = "#26405A";

    private const string DarkSunken = "#081522";

    public static MudTheme Create() => new()
    {
        // The *Darken tones are the hover of a fill: 8% brighter (each channel x1.08) while the fill's contrast text
        // keeps 4.5:1 there, else 8% darker. Light green and light info carry white text that no brighter tone keeps.
        PaletteLight = new PaletteLight
        {
            Primary = "#064F80",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#06558A",
            // 17. Tint blue: informative highlights, the role badge, a selected radio card, the editor's pressed tool.
            PrimaryLighten = "#DCEBF7",
            Secondary = "#00806B",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#007662",
            // Pinned: MudBlazor's derivation of this tone differs between runtimes (no kit component paints with it).
            SecondaryLighten = "#00A388",
            Tertiary = "#E8650A",
            TertiaryContrastText = Ink,
            TertiaryDarken = "#FB6D0B",
            Info = "#0872B8",
            InfoContrastText = "#FFFFFF",
            InfoDarken = "#0769A9",
            Success = "#00806B",
            SuccessContrastText = "#FFFFFF",
            SuccessDarken = "#007662",
            SuccessLighten = "#00A388",
            Warning = "#D98B1A",
            WarningContrastText = Ink,
            WarningDarken = "#EA961C",
            Error = "#BE3737",
            ErrorContrastText = "#FFFFFF",
            ErrorDarken = "#CD3B3B",
            Background = "#F4F6F9",
            Surface = "#FFFFFF",
            BackgroundGray = Sunken,
            ActionDisabledBackground = Sunken,
            ActionDisabled = InkMuted,
            TextPrimary = Ink,
            TextSecondary = InkMuted,
            ActionDefault = InkMuted,
            LinesInputs = "#6B7C91",
            LinesDefault = Line,
            Divider = Line,
            TableLines = Line,
            AppbarBackground = "#FFFFFF",
            AppbarText = Ink,
            // 15. The sidebar keeps Simulab's navy.
            DrawerBackground = "#0F3266",
            // White at 0.92, written as the byte MudColor keeps (0xEB): its own rgba() parse truncates 234.6 to 0xEA.
            DrawerText = "#FFFFFFEB",
            DrawerIcon = "#FFFFFFEB",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2AA8F0",
            PrimaryContrastText = Ink,
            PrimaryDarken = "#2DB5FF",
            PrimaryLighten = "#16344F",
            Secondary = "#00A68C",
            SecondaryContrastText = Ink,
            SecondaryDarken = "#00B397",
            SecondaryLighten = "#00CCAD",
            Tertiary = "#FF7A2E",
            TertiaryContrastText = Ink,
            TertiaryDarken = "#FF8432",
            Info = "#2AA8F0",
            InfoContrastText = Ink,
            InfoDarken = "#2DB5FF",
            Success = "#00A68C",
            SuccessContrastText = Ink,
            SuccessDarken = "#00B397",
            SuccessLighten = "#00CCAD",
            Warning = "#D98B1A",
            WarningContrastText = Ink,
            WarningDarken = "#EA961C",
            Error = "#E06C6C",
            ErrorContrastText = Ink,
            ErrorDarken = "#F27575",
            Background = DarkPage,
            Surface = DarkRaised,
            BackgroundGray = DarkSunken,
            ActionDisabledBackground = DarkSunken,
            ActionDisabled = DarkInkMuted,
            TextPrimary = DarkInk,
            TextSecondary = DarkInkMuted,
            ActionDefault = DarkInkMuted,
            LinesInputs = "#6A829A",
            LinesDefault = DarkLine,
            Divider = DarkLine,
            TableLines = DarkLine,
            AppbarBackground = DarkRaised,
            AppbarText = DarkInk,
            // The navy against the dark page reads at 1.38:1, so the dark sidebar is the sunken surface (F-125 decisions).
            DrawerBackground = DarkSunken,
            DrawerText = DarkInkMuted,
            DrawerIcon = DarkInkMuted,
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Bricolage Grotesque", "system-ui", "sans-serif"],
                FontWeight = "500",
            },
            // ui-title, ui-heading and ui-subheading: titles are 800 with a negative letter spacing.
            H1 = new H1Typography { FontSize = "1.75rem", FontWeight = "800", LineHeight = "2rem", LetterSpacing = "-0.02em" },
            H2 = new H2Typography { FontSize = "1.25rem", FontWeight = "800", LineHeight = "1.5rem", LetterSpacing = "-0.01em" },
            H3 = new H3Typography { FontSize = "1rem", FontWeight = "800", LineHeight = "1.25rem", LetterSpacing = "-0.01em" },
            Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "800", LineHeight = "1.25rem", LetterSpacing = "-0.01em" },
            // ui-body and ui-control: no letter spacing.
            Body1 = new Body1Typography { FontSize = "0.9375rem", LineHeight = "1.375rem", LetterSpacing = "0" },
            Body2 = new Body2Typography { FontSize = "0.875rem", LineHeight = "1.25rem", LetterSpacing = "0" },
            Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = "800", LineHeight = "1.25rem", LetterSpacing = "0", TextTransform = "none" },
            // ui-caption and ui-label.
            Caption = new CaptionTypography { FontSize = "0.75rem", LineHeight = "1rem", LetterSpacing = "0" },
            Overline = new OverlineTypography { FontSize = "0.75rem", FontWeight = "800", LineHeight = "1rem", LetterSpacing = "0.04em" },
        },
        // BR6: no diffuse shadow anywhere; app.css draws a 1px line around each surface instead.
        Shadows = new Shadow { Elevation = [.. Enumerable.Repeat("none", 26)] },
        // BR7: buttons and fields; tags (6px) and cards and dialogs (20px) take theirs from app.css.
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px" },
    };
}
