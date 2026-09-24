namespace Simulab.Catalog.Contracts;

/// <summary>One organizer, as every caller of the catalog sees it (F-33).</summary>
public sealed record OrganizerResponse(
    Guid Id,
    string Name,
    string Acronym,
    OrganizerKind Kind,
    string? Description,
    string? Website);
