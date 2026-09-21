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
            Secondary = "#1A9E8C",
            SecondaryDarken = "#137A6B",
            Tertiary = "#F5A020",
            Background = "#F2F5FB",
            Surface = "#FFFFFF",
            DrawerBackground = "#0F3266",
            DrawerText = "rgba(255,255,255,0.92)",
            AppbarBackground = "#0F3266",
            AppbarText = "#FFFFFF",
            TextPrimary = "#19243E",
            TextSecondary = "#4A5A7A",
            ActionDefault = "#8A96B0",
            Divider = "#D0D8EA",
            TableLines = "#D0D8EA",
            Success = "#1A9E8C",
            Warning = "#D98B1A",
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
            Secondary = "#1A9E8C",
            Tertiary = "#F5A020",
            Background = "#0E1828",
            Surface = "#172035",
            DrawerBackground = "#091422",
            AppbarBackground = "#091422",
            TextPrimary = "#E0E8F8",
            TextSecondary = "#7A8EB0",
            ActionDefault = "#404E6A",
            Divider = "#263352",
            TableLines = "#263352",
            Success = "#1A9E8C",
            Warning = "#D98B1A",
            // F-10: the light red is 2.93:1 on the dark surface, so error text failed AA there. This one is
            // 5.05:1 on the surface and 5.53:1 on the background; the filled button gets dark ink instead of
            // white, which white could no longer give on a lighter red. ThemeContrastTests holds the numbers.
            Error = "#E06C6C",
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
