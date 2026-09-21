namespace Simulab.Identity.Contracts;

/// <summary>
/// The whole file of F-16 (BR3). Each module owns one top-level section named after it; a later module adds
/// its own section here without changing the others.
/// </summary>
public sealed record DataExportResponse(string Format, int Version, DateTimeOffset ExportedAt, IdentityDataResponse Identity)
{
    /// <summary>The value of <see cref="Format"/>: tells a reader what the file is.</summary>
    public const string FormatName = "simulab.data-export";

    /// <summary>The value of <see cref="Version"/>; it changes only when an existing field changes meaning.</summary>
    public const int CurrentVersion = 1;
}
