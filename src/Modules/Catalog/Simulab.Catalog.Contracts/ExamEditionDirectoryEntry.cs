namespace Simulab.Catalog.Contracts;

/// <summary>
/// An exam edition as another module may see it (F-96): the parts a source line is built from, with its
/// exam and organizer, and its status. Never the notice reference, url or applied-on date.
/// </summary>
/// <param name="Id">The edition.</param>
/// <param name="ExamId">The exam it belongs to.</param>
/// <param name="ExamName">The exam's name.</param>
/// <param name="OrganizerId">The organizer that ran the exam.</param>
/// <param name="OrganizerAcronym">The organizer's acronym.</param>
/// <param name="OrganizerName">The organizer's name.</param>
/// <param name="NoticeYear">The year of the notice.</param>
/// <param name="Position">The position the edition was for, when it has one.</param>
/// <param name="Status">Draft or Published.</param>
public sealed record ExamEditionDirectoryEntry(
    Guid Id,
    Guid ExamId,
    string ExamName,
    Guid OrganizerId,
    string OrganizerAcronym,
    string OrganizerName,
    int NoticeYear,
    string? Position,
    ExamEditionStatus Status);
