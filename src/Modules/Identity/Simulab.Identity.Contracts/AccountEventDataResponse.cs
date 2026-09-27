namespace Simulab.Identity.Contracts;

/// <summary>
/// One security event of the account in the data export (F-32 BR1), the same fields the admin trail already
/// translates (F-21), raw here: the export is machine-readable JSON, like the rest of the file (F-16 BR3).
/// </summary>
public sealed record AccountEventDataResponse(
    DateTimeOffset OccurredAt,
    string Type,
    string? Method,
    string? Reason,
    string? IpAddress);
