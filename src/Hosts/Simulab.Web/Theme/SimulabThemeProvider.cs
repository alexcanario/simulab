using System.Globalization;
using System.Text;
using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>
/// F-125 and F-148: MudBlazor's theme provider plus the colours its palette has no slot for (<see cref="KeycapEdges"/>, the
/// link role and the brand name), written in the same <c>:root</c> block and switched with the same dark mode, so every
/// colour still comes from SimulabTheme (BR2).
/// </summary>
public sealed class SimulabThemeProvider : MudThemeProvider
{
    protected override void GenerateTheme(StringBuilder themeStringBuilder)
    {
        ArgumentNullException.ThrowIfNull(themeStringBuilder);
        base.GenerateTheme(themeStringBuilder);

        // MUD0010 guards a component against reading a parameter it may also write; this provider only reads it, while
        // rendering, after the framework has set it — the moment the base class reads it for its own palette.
#pragma warning disable MUD0010
        var dark = IsDarkMode;
#pragma warning restore MUD0010
        var edges = dark ? SimulabTheme.DarkKeycapEdges : SimulabTheme.LightKeycapEdges;
        var culture = CultureInfo.InvariantCulture;
        themeStringBuilder.AppendLine(culture, $"--app-keycap-primary-edge: {edges.Primary};");
        themeStringBuilder.AppendLine(culture, $"--app-keycap-secondary-edge: {edges.Secondary};");
        themeStringBuilder.AppendLine(culture, $"--app-keycap-tertiary-edge: {edges.Tertiary};");
        themeStringBuilder.AppendLine(culture, $"--app-keycap-info-edge: {edges.Info};");
        themeStringBuilder.AppendLine(culture, $"--app-keycap-error-edge: {edges.Error};");
        themeStringBuilder.AppendLine(culture, $"--app-link: {(dark ? SimulabTheme.DarkLink : SimulabTheme.LightLink)};");
        themeStringBuilder.AppendLine(culture, $"--app-primary-selected: {(dark ? SimulabTheme.DarkPrimarySelected : SimulabTheme.LightPrimarySelected)};");
        themeStringBuilder.AppendLine(culture, $"--app-brand-name: {(dark ? SimulabTheme.DarkBrandName : SimulabTheme.LightBrandName)};");
    }
}
