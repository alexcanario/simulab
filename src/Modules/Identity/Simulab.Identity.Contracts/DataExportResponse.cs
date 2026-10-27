using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// The member names the file already uses: a section named like one of them would overwrite it (F-167 BR9).
    /// </summary>
    public static IReadOnlyList<string> ReservedNames { get; } = ["format", "version", "exportedAt", "identity"];

    /// <summary>
    /// The other modules' sections (<see cref="IDataExportSection"/>, F-167 BR9), each written as a top-level member
    /// under its own name, after <see cref="Identity"/>. Nothing is written when there is none.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Sections { get; init; }
}
