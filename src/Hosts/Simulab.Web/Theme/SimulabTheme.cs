using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>Started from Simulae's theme (ADR-0001, decision 30). Revisit when Simulab has its own brand.</summary>
public static class SimulabTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // B-8: primary is also a text colour (outlined buttons, the app name). #2478C5 read at 4.21:1 on the
            // background; this one is 4.91:1 there, 5.36:1 on the surface. ThemeContrastTests holds the numbers.
            Primary = "#216DB5",
            PrimaryDarken = "#1858A0",
            PrimaryLighten = "#EBF3FD",
            // F-17: white on the teal and the amber read at 3.33:1 and 2.11:1, so both fills get the dark ink
            // success and warning have (B-9). That ink reads 3.00:1 on a darker teal, so the teal's hover tone
            // is lighter instead. ThemePaletteTests holds every pair of the palette.
            Secondary = "#1A9E8C",
            SecondaryDarken = "#24AE9A",
            SecondaryContrastText = "#19243E",
            Tertiary = "#F5A020",
            TertiaryContrastText = "#19243E",
            Background = "#F2F5FB",
            Surface = "#FFFFFF",
            DrawerBackground = "#0F3266",
            DrawerText = "rgba(255,255,255,0.92)",
            // F-17: app.css paints the nav icons in the drawer text colour; the token now says the same (it was
            // a grey at 2.03:1 on the drawer).
            DrawerIcon = "rgba(255,255,255,0.92)",
            AppbarBackground = "#0F3266",
            AppbarText = "#FFFFFF",
            TextPrimary = "#19243E",
            TextSecondary = "#4A5A7A",
            // B-10: the colour of every icon-only button and menu icon. #8A96B0 read at 2.97:1 on the surface; the
            // secondary text colour is 6.92:1 there, 6.34:1 on the background. ThemeContrastTests holds the numbers.
            ActionDefault = "#4A5A7A",
            // F-17: the border of every outlined field. The grey default read at 1.88:1 on the surface and 1.72:1
            // on the background, under the 3:1 a control boundary needs (WCAG 2.2 1.4.11).
            LinesInputs = "#7E8AA1",
            Divider = "#D0D8EA",
            TableLines = "#D0D8EA",
            // F-17: a filled success or warning button keeps its dark ink on hover; the default darker tones read
            // at 3.00:1 and 4.02:1 under it.
            Success = "#1A9E8C",
            SuccessDarken = "#24AE9A",
            Warning = "#D98B1A",
            WarningDarken = "#CF8419",
            Error = "#BE3737",
            Info = "#2478C5",
            // B-9: the kit's alert is filled, so this is the text on top of Success and Warning. The dark teal
            // read at 2.24:1 there and the brown at 2.73:1; this ink is 4.63:1 and 5.62:1. ThemeContrastTests
            // holds the numbers.
            SuccessContrastText = "#19243E",
            WarningContrastText = "#19243E",
            // F-10: this is the text of a filled destructive button, on top of Error. The dark red it had
            // read at 1.92:1 there; white reads at 5.54:1. ThemeContrastTests holds the numbers.
            ErrorContrastText = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            // B-8: the light blue was 3.53:1 as text on the dark surface. This one is 6.04:1 there and 6.63:1 on the
            // background; white cannot sit on it, so filled buttons get dark ink, and the hover tone stays light
            // enough for that ink. ThemeContrastTests holds the numbers.
            Primary = "#64A2E3",
            PrimaryDarken = "#5096DC",
            PrimaryContrastText = "#19243E",
            PrimaryLighten = "#162540",
            // F-17: the same teal and amber as the light theme, with the same dark ink and lighter teal hover.
            Secondary = "#1A9E8C",
            SecondaryDarken = "#24AE9A",
            SecondaryContrastText = "#19243E",
            Tertiary = "#F5A020",
            TertiaryContrastText = "#19243E",
            Background = "#0E1828",
            Surface = "#172035",
            DrawerBackground = "#091422",
            // F-17: MudBlazor's dark drawer text, written down so the icon token can say the same (see light).
            DrawerText = "rgba(255,255,255,0.5)",
            DrawerIcon = "rgba(255,255,255,0.5)",
            AppbarBackground = "#091422",
            TextPrimary = "#E0E8F8",
            TextSecondary = "#7A8EB0",
            // B-10: the dark icon colour read at 1.94:1 on the surface, so row actions and menu icons almost
            // vanished. The secondary text colour is 4.89:1 there, 5.36:1 on the background. ThemeContrastTests
            // holds the numbers.
            ActionDefault = "#7A8EB0",
            // F-17: translucent white at 30% read at 2.67:1 as a field border on both dark surfaces.
            LinesInputs = "#61718F",
            Divider = "#263352",
            TableLines = "#263352",
            Success = "#1A9E8C",
            SuccessDarken = "#24AE9A",
            Warning = "#D98B1A",
            WarningDarken = "#CF8419",
            // F-10: the light red is 2.93:1 on the dark surface, so error text failed AA there. This one is
            // 5.05:1 on the surface and 5.53:1 on the background; the filled button gets dark ink instead of
            // white, which white could no longer give on a lighter red. ThemeContrastTests holds the numbers.
            Error = "#E06C6C",
            // F-17: the filled destructive button's hover; the default darker red read at 3.80:1 under the ink.
            ErrorDarken = "#DE6868",
            ErrorContrastText = "#19243E",
            Info = "#2478C5",
            // B-9: white on the teal was 3.33:1 and on the amber 2.74:1, so a filled success or warning alert
            // failed AA here too. The dark ink is 4.63:1 and 5.62:1. ThemeContrastTests holds the numbers.
            SuccessContrastText = "#19243E",
            WarningContrastText = "#19243E",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "sans-serif"]
            },
            H1 = new H1Typography { FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.2" },
            H2 = new H2Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.3" },
            Body1 = new Body1Typography { FontSize = "0.9375rem", LineHeight = "1.65" },
            Body2 = new Body2Typography { FontSize = "0.8125rem", LineHeight = "1.5" },
            Caption = new CaptionTypography { FontSize = "0.75rem" },
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "10px" },
    };
}
