namespace Simulab.Catalog.Contracts;

/// <summary>
/// One edition of an exam, as every caller of the catalog sees it (F-35). The board travels with its name
/// and acronym as well as its id: the section row shows the acronym, and one call is enough to draw it.
/// <paramref name="NoticeDocument"/> is the notice PDF the edition keeps (F-89), null when it has none.
/// </summary>
public sealed record ExamEditionResponse(
    Guid Id,
    Guid ExamId,
    Guid OrganizerId,
    string OrganizerName,
    string OrganizerAcronym,
    int NoticeYear,
    string? Position,
    string? NoticeReference,
    string? NoticeUrl,
    DateOnly? AppliedOn,
    ExamEditionStatus Status,
    NoticeDocumentResponse? NoticeDocument = null);
