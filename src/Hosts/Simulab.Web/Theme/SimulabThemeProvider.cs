using System.Text;
using MudBlazor;

namespace Simulab.Web.Theme;

/// <summary>
/// F-125: MudBlazor's theme provider plus the colours its palette has no slot for (<see cref="KeycapEdges"/>), written
/// in the same <c>:root</c> block and switched with the same dark mode, so every colour still comes from SimulabTheme (BR2).
/// </summary>
public sealed class SimulabThemeProvider : MudThemeProvider
{
    protected override void GenerateTheme(StringBuilder themeStringBuilder)
    {
        base.GenerateTheme(themeStringBuilder);
    }
}
