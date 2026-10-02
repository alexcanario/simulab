namespace Simulab.Catalog.Contracts;

/// <summary>A published exam with every one of its published editions, newest year first (F-36, BR8).</summary>
public sealed record PublishedExamDetailResponse(
    Guid Id,
    string Name,
    string IssuingAuthorityName,
    AssessmentType AssessmentType,
    ExamScope Scope,
    string? ScopeDetail,
    string ContentLanguage,
    int PublishedEditionCount,
    int LatestNoticeYear,
    IReadOnlyList<PublishedExamEditionResponse> Editions);
