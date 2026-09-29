namespace Simulab.Catalog.Contracts;

/// <summary>An exam board that names at least one published edition: what the board filter offers (F-36, BR7).</summary>
public sealed record PublishedExamOrganizerResponse(Guid Id, string Name, string Acronym);
