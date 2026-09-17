using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>Started from Simulae's theme (ADR-0001, decision 30). Revisit when Simulab has its own brand.</summary>
public static class SimulabTheme
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2478C5",
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
            SuccessContrastText = "#0D6056",
            WarningContrastText = "#7A4A00",
            ErrorContrastText = "#7A1818",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2478C5",
            PrimaryDarken = "#1858A0",
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
            Error = "#BE3737",
            Info = "#2478C5",
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
