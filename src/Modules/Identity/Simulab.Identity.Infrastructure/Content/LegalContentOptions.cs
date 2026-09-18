namespace Simulab.Identity.Infrastructure.Content;

/// <summary>Where the institutional documents live. Bound from configuration section <c>Legal</c>.</summary>
public sealed class LegalContentOptions
{
    public const string SectionName = "Legal";

    /// <summary>
    /// Root folder holding <c>&lt;locale&gt;/&lt;topic&gt;/manifest.json</c>. Defaults to the copy shipped
    /// next to the application.
    /// </summary>
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Content", "Legal");
}
