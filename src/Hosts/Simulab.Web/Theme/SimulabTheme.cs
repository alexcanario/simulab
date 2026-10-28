using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>
/// The Control Alt Systems identity, palette v2 (F-148, ADR-0007; the identity itself is F-125, ADR-0006): the palette of the
/// item's approved list, Bricolage Grotesque and JetBrains Mono, keycap buttons, borders instead of shadows.
/// ApprovedPaletteTests holds every value; ThemePaletteTests and KeycapContrastTests hold every pair;
/// IdentityTokensTests keeps docs/design in step.
/// </summary>
public static class SimulabTheme
{
    /// <summary>The edge under each filled keycap, light theme. The primary edge is the design system's line-strong; error is error x0.72.</summary>
    public static KeycapEdges LightKeycapEdges { get; } = new(Primary: "#6B7C91", Secondary: "#107264", Tertiary: "#B87410", Info: "#17528A", Error: "#842424");

    /// <summary>The same in the dark theme, error x0.65. Only the amber edge identifies a button (light theme, BR3); the others are decorative.</summary>
    public static KeycapEdges DarkKeycapEdges { get; } = new(Primary: "#1F6FB5", Secondary: "#107264", Tertiary: "#B87410", Info: "#1F6FB5", Error: "#A66560");

    /// <summary>The link role has no slot in MudBlazor's palette: <see cref="SimulabThemeProvider"/> writes it as <c>--app-link</c> (BR6).</summary>
    public static string LightLink => "#1D64A8";

    public static string DarkLink => "#8CC4F5";

    /// <summary>The fill of a marked answer (F-169): <see cref="SimulabThemeProvider"/> writes it as <c>--app-primary-selected</c>.</summary>
    public static string LightPrimarySelected => "#D1E4FE";

    public static string DarkPrimarySelected => "#1A4583";

    /// <summary>The brand name in the app bar, also the plus signs of the action shortcut: navy in light, ink in dark (BR9).</summary>
    public static string LightBrandName => "#0F3266";

    public static string DarkBrandName => "#F2F5FB";

    private const string Ink = "#0B1B2E";

    private const string InkMuted = "#52647A";

    private const string Line = "#D9E1EE";

    private const string Sunken = "#E6ECF5";

    private const string DarkPage = "#0B2149";

    private const string DarkRaised = "#12386F";

    private const string DarkInk = "#F2F5FB";

    private const string DarkInkMuted = "#B8C4D2";

    private const string DarkLine = "#24497F";

    private const string DarkSunken = "#081938";

    private const string Teal = "#1A9E8C";

    public static MudTheme Create() => new()
    {
        // The *Darken tones are the hover of a fill: 8% brighter (each channel x1.08) while the fill's contrast text
        // keeps 4.5:1 there, else 8% darker. Light info carries white text that no brighter tone keeps.
        PaletteLight = new PaletteLight
        {
            Primary = "#0F3266",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#10366E",
            // 18. Tint blue: informative highlights, the role badge, a selected radio card, the editor's pressed tool.
            PrimaryLighten = "#DCE8F5",
            Secondary = Teal,
            SecondaryContrastText = Ink,
            SecondaryDarken = "#1CAB97",
            // Pinned: MudBlazor's derivation of this tone differs between runtimes (no kit component paints with it).
            SecondaryLighten = "#21C5AF",
            Tertiary = "#F5A020",
            TertiaryContrastText = Ink,
            TertiaryDarken = "#FFAD23",
            Info = "#2478C5",
            InfoContrastText = "#FFFFFF",
            InfoDarken = "#216EB5",
            Success = Teal,
            SuccessContrastText = Ink,
            SuccessDarken = "#1CAB97",
            SuccessLighten = "#21C5AF",
            Warning = "#D98B1A",
            WarningContrastText = Ink,
            WarningDarken = "#EA961C",
            Error = "#B83232",
            ErrorContrastText = "#FFFFFF",
            ErrorDarken = "#C73636",
            Background = "#F2F5FB",
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
            // 16. The sidebar is the brand navy in the light theme.
            DrawerBackground = "#0F3266",
            // White at 0.92, written as the byte MudColor keeps (0xEB): its own rgba() parse truncates 234.6 to 0xEA.
            DrawerText = "#FFFFFFEB",
            DrawerIcon = "#FFFFFFEB",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#47A0EB",
            PrimaryContrastText = Ink,
            PrimaryDarken = "#4DADFE",
            PrimaryLighten = "#1A4583",
            Secondary = Teal,
            SecondaryContrastText = Ink,
            SecondaryDarken = "#1CAB97",
            SecondaryLighten = "#21C5AF",
            Tertiary = "#F5A020",
            TertiaryContrastText = Ink,
            TertiaryDarken = "#FFAD23",
            Info = "#47A0EB",
            InfoContrastText = Ink,
            InfoDarken = "#4DADFE",
            Success = Teal,
            SuccessContrastText = Ink,
            SuccessDarken = "#1CAB97",
            SuccessLighten = "#21C5AF",
            Warning = "#D98B1A",
            WarningContrastText = Ink,
            WarningDarken = "#EA961C",
            Error = "#FF9C94",
            ErrorContrastText = Ink,
            ErrorDarken = "#FFA8A0",
            Background = DarkPage,
            Surface = DarkRaised,
            BackgroundGray = DarkSunken,
            ActionDisabledBackground = DarkSunken,
            ActionDisabled = DarkInkMuted,
            TextPrimary = DarkInk,
            TextSecondary = DarkInkMuted,
            ActionDefault = DarkInkMuted,
            LinesInputs = "#7F9BC0",
            LinesDefault = DarkLine,
            Divider = DarkLine,
            TableLines = DarkLine,
            AppbarBackground = DarkRaised,
            AppbarText = DarkInk,
            // The navy against the dark page reads at 1.26:1, so the dark sidebar is the sunken surface (F-125 decisions).
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
