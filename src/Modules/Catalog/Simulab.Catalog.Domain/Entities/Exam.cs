using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// An assessment its issuing authority contracts and publishes a notice for (F-34): a public service exam,
/// a certification, a university entrance exam or ENEM. The exam belongs to the body that defines the
/// positions, the syllabus and the rules (BR2); the board that elaborates and applies one paper belongs to
/// the edition, because the same exam changes board between editions.
/// <para>
/// Global data — <see cref="TenantEntity.TenantId"/> is null in v1 (BR1) — and the rules here answer with a
/// <see cref="Result"/> and never throw (BR15). Uniqueness is not one of them: it needs the table (BR10,
/// checked by the handler and by the unique index).
/// </para>
/// </summary>
public sealed class Exam : TenantEntity
{
    private Exam()
    {
    }

    /// <summary>The organizer that publishes the notice and sets the rules (BR2).</summary>
    public Guid IssuingAuthorityId { get; private set; }

    /// <summary>The exam's name, unique inside its issuing authority (BR10).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>What kind of assessment this is (BR6).</summary>
    public AssessmentType AssessmentType { get; private set; }

    /// <summary>How far the exam reaches (BR7).</summary>
    public ExamScope Scope { get; private set; }

    /// <summary>Which state or which municipality; null when the scope is national (BR8).</summary>
    public string? ScopeDetail { get; private set; }

    /// <summary>The language the exam and its questions are written in; never translated (BR9, ADR-0001 #27).</summary>
    public string ContentLanguage { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the search read (BR10, BR14).</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<Exam> Create(
        Guid? issuingAuthorityId,
        string? name,
        AssessmentType assessmentType,
        ExamScope scope,
        string? scopeDetail,
        string? contentLanguage)
    {
        var exam = new Exam();
        var applied = exam.Apply(issuingAuthorityId, name, assessmentType, scope, scopeDetail, contentLanguage);

        return applied.IsFailure ? Result.Failure<Exam>(applied.Error!) : Result.Success(exam);
    }

    public Result Update(
        Guid? issuingAuthorityId,
        string? name,
        AssessmentType assessmentType,
        ExamScope scope,
        string? scopeDetail,
        string? contentLanguage) =>
        Apply(issuingAuthorityId, name, assessmentType, scope, scopeDetail, contentLanguage);

    private Result Apply(
        Guid? issuingAuthorityId,
        string? name,
        AssessmentType assessmentType,
        ExamScope scope,
        string? scopeDetail,
        string? contentLanguage)
    {
        if (issuingAuthorityId is not { } authority || authority == Guid.Empty)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamIssuingAuthorityRequired, ErrorKind.Validation));
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamNameRequired, ErrorKind.Validation));
        }

        if (trimmedName.Length > CatalogLimits.ExamNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamNameTooLong, ErrorKind.Validation));
        }

        if (!Enum.IsDefined(assessmentType))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamAssessmentTypeInvalid, ErrorKind.Validation));
        }

        if (!Enum.IsDefined(scope))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamScopeInvalid, ErrorKind.Validation));
        }

        // BR8: a national exam has no detail, whatever the request sent; the other two must say where.
        var trimmedDetail = string.IsNullOrWhiteSpace(scopeDetail) ? null : scopeDetail.Trim();
        if (scope == ExamScope.National)
        {
            trimmedDetail = null;
        }
        else if (trimmedDetail is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamScopeDetailRequired, ErrorKind.Validation));
        }
        else if (trimmedDetail.Length > CatalogLimits.ExamScopeDetailMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamScopeDetailTooLong, ErrorKind.Validation));
        }

        // BR9: one list of languages for the whole app, so the content language and the UI can never drift
        // apart. It lives in Identity's contracts because F-8 put it there; a module may read another
        // module's contracts (F-33 BR1), and this reads nothing else from Identity.
        var language = SupportedLanguages.Canonical(contentLanguage);
        if (language is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamContentLanguageInvalid, ErrorKind.Validation));
        }

        IssuingAuthorityId = authority;
        Name = trimmedName;
        AssessmentType = assessmentType;
        Scope = scope;
        ScopeDetail = trimmedDetail;
        ContentLanguage = language;
        NormalizedName = CatalogText.Normalize(trimmedName);

        return Result.Success();
    }
}
