namespace Simulab.Catalog.Contracts;

/// <summary>
/// One published edition on the exam page (F-36, BR8). It is always published, so it carries no status.
/// <paramref name="NoticeSubjects"/> is its syllabus in display order (F-76, BR1), empty when none is registered;
/// null is read as empty.
/// </summary>
public sealed record PublishedExamEditionResponse(
    Guid Id,
    int NoticeYear,
    string? Position,
    string OrganizerName,
    string OrganizerAcronym,
    string? NoticeReference,
    string? NoticeUrl,
    DateOnly? AppliedOn,
    IReadOnlyList<NoticeSubjectResponse>? NoticeSubjects = null);
