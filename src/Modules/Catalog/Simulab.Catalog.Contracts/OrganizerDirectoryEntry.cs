namespace Simulab.Catalog.Contracts;

/// <summary>A live organizer as another module sees it (F-100): the names a filter shows.</summary>
/// <param name="Id">The organizer.</param>
/// <param name="Acronym">The short name (CEBRASPE, FGV).</param>
/// <param name="Name">The full name.</param>
public sealed record OrganizerDirectoryEntry(Guid Id, string Acronym, string Name);
