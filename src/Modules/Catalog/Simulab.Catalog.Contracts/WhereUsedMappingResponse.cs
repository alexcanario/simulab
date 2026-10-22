namespace Simulab.Catalog.Contracts;

/// <summary>
/// One live notice subject that maps a subject whole or a topic (F-90, BR5): its exam, its edition and its own
/// name, with the ids the screen needs to link to the edition page, where the mapping is edited (F-75).
/// </summary>
/// <param name="Position">The edition's position, when it has one.</param>
public sealed record WhereUsedMappingResponse(
    Guid ExamId,
    string ExamName,
    Guid EditionId,
    int NoticeYear,
    string? Position,
    Guid NoticeSubjectId,
    string NoticeSubjectName);
