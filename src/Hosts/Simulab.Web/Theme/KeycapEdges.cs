namespace Simulab.Web.Theme;

/// <summary>
/// F-125: the colour of the keycap edge under each filled button variant. MudBlazor's palette has no slot for it,
/// so <see cref="SimulabThemeProvider"/> writes these as <c>--app-keycap-*-edge</c> next to the palette's own variables.
/// The edge is decorative (change note v2): the fill is what identifies the button.
/// </summary>
public sealed record KeycapEdges(string Primary, string Secondary, string Tertiary, string Info, string Error);
