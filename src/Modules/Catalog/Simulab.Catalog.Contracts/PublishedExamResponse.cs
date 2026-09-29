namespace Simulab.Catalog.Contracts;

/// <summary>One row of the student catalog: a published exam and what its published editions add up to (F-36, BR2).</summary>
public sealed record PublishedExamResponse(
    Guid Id,
    string Name,
    string IssuingAuthorityName,
    string IssuingAuthorityAcronym,
    AssessmentType AssessmentType,
    ExamScope Scope,
    string? ScopeDetail,
    string ContentLanguage,
    int PublishedEditionCount,
    int LatestNoticeYear);
