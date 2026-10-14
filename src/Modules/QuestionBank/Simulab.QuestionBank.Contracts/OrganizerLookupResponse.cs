namespace Simulab.QuestionBank.Contracts;

/// <summary>An organizer the question list filter offers (F-100): its acronym first, then its full name.</summary>
/// <param name="Id">The organizer.</param>
/// <param name="Acronym">The short name (CEBRASPE, FGV).</param>
/// <param name="Name">The full name.</param>
public sealed record OrganizerLookupResponse(Guid Id, string Acronym, string Name);
