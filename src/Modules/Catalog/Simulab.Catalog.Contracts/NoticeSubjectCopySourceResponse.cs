namespace Simulab.Catalog.Contracts;

/// <summary>
/// An edition whose notice subjects can be copied into another edition of the same exam (F-86, BR1): what the
/// copy dialog lists, newest notice year first. <paramref name="NoticeSubjectCount"/> counts the live rows.
/// </summary>
public sealed record NoticeSubjectCopySourceResponse(
    Guid EditionId,
    int NoticeYear,
    string? Position,
    string OrganizerName,
    int NoticeSubjectCount);
