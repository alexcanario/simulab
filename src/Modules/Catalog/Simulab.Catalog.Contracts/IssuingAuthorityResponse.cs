namespace Simulab.Catalog.Contracts;

/// <summary>One issuing authority, as every caller of the catalog sees it (F-34 BR18, v2).</summary>
public sealed record IssuingAuthorityResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Website);
