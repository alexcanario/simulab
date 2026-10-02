namespace Simulab.Catalog.Contracts;

/// <summary>
/// One exam, as every caller of the catalog sees it (F-34). The issuing authority travels with its name
/// as well as its id: the list shows it, and one call is enough to draw a page.
/// </summary>
public sealed record ExamResponse(
    Guid Id,
    string Name,
    Guid IssuingAuthorityId,
    string IssuingAuthorityName,
    AssessmentType AssessmentType,
    ExamScope Scope,
    string? ScopeDetail,
    string ContentLanguage);
