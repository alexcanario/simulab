namespace Simulab.Catalog.Contracts;

/// <summary>
/// A notice subject as the edition page shows it (F-74). The list comes in display order, which is the
/// contract: there is no position field. <paramref name="Mappings"/> is the canonical subjects and topics it
/// covers (F-75), empty when it is not mapped; null is read as empty.
/// </summary>
public sealed record NoticeSubjectResponse(
    Guid Id,
    Guid ExamEditionId,
    string? Group,
    string Label,
    int? QuestionCount,
    IReadOnlyList<NoticeSubjectMappingResponse>? Mappings = null);
