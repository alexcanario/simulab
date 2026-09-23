namespace Simulab.Catalog.Contracts;

/// <summary>What the add and the edit dialog send (F-33, UC2 and UC3).</summary>
public sealed record SaveOrganizerRequest(
    string? Name,
    string? Acronym,
    OrganizerKind Kind,
    string? Description = null,
    string? Website = null);
