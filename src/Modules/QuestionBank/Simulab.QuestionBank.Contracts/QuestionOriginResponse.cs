namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// Where a question came from (F-96): the edition, its number in the paper and, optionally, its notice
/// subject. The exam, organizer and year are read from the edition each time and never stored on the
/// question (BR4). When the edition no longer resolves the names are empty and the year is null (BR11).
/// </summary>
/// <param name="ExamEditionId">The edition the question was taken from.</param>
/// <param name="ExamId">The exam of that edition.</param>
/// <param name="ExamName">The exam's name, empty when the edition no longer resolves.</param>
/// <param name="OrganizerAcronym">The organizer's acronym, empty when the edition no longer resolves.</param>
/// <param name="OrganizerName">The organizer's name, empty when the edition no longer resolves.</param>
/// <param name="NoticeYear">The year of the notice, or null when the edition no longer resolves.</param>
/// <param name="Position">The position the edition was for, when it has one.</param>
/// <param name="PaperNumber">The number of the question in the paper, from 1 to 500.</param>
/// <param name="NoticeSubjectId">The notice subject the question belongs to, when it has one.</param>
/// <param name="NoticeSubjectLabel">The notice subject's label, when it has one and it resolves.</param>
public sealed record QuestionOriginResponse(
    Guid ExamEditionId,
    Guid ExamId,
    string ExamName,
    string OrganizerAcronym,
    string OrganizerName,
    int? NoticeYear,
    string? Position,
    int PaperNumber,
    Guid? NoticeSubjectId = null,
    string? NoticeSubjectLabel = null);
