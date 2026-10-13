namespace Simulab.Catalog.Contracts;

/// <summary>The live editions of one organizer (F-100), by id: what a question filter by organizer needs.</summary>
/// <param name="OrganizerId">The organizer.</param>
/// <param name="ExamEditionIds">Its live editions of any status; empty when it has none.</param>
public sealed record OrganizerEditionsDirectoryEntry(Guid OrganizerId, IReadOnlyList<Guid> ExamEditionIds);
