namespace Simulab.QuestionBank.Contracts;

/// <summary>One edition offered by the origin lookup of the question form, of any status (F-96, BR9).</summary>
/// <param name="Id">The edition.</param>
/// <param name="ExamName">The exam's name.</param>
/// <param name="OrganizerAcronym">The organizer's acronym.</param>
/// <param name="NoticeYear">The year of the notice.</param>
/// <param name="Position">The position, when the edition has one.</param>
/// <param name="Status">Draft or Published, as the catalog names them. A string because a contracts project
/// references the shared kernel only, never another module's contracts.</param>
public sealed record ExamEditionLookupResponse(
    Guid Id,
    string ExamName,
    string OrganizerAcronym,
    int NoticeYear,
    string? Position,
    string Status);
