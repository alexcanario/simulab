using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// One paper actually applied (F-35): the year of its notice, the job it selects for, the board that applied
/// it, and whether students can see it. The edition is where the exam board lives, because the same exam
/// changes board between years (BR3).
/// <para>
/// Global data — <see cref="TenantEntity.TenantId"/> is null in v1 (BR1) — and the rules here answer with a
/// <see cref="Result"/> and never throw (BR15). Uniqueness is not one of them: it needs the table (BR10,
/// checked by the handler and by the unique index). The upper bound of the notice year comes from the
/// caller, so this project never reads a clock.
/// </para>
/// </summary>
public sealed class ExamEdition : TenantEntity
{
    private ExamEdition()
    {
    }

    /// <summary>The exam this paper belongs to; fixed at creation (BR2).</summary>
    public Guid ExamId { get; private set; }

    /// <summary>The exam board that applied the paper (BR3).</summary>
    public Guid OrganizerId { get; private set; }

    /// <summary>The year of the notice (BR4).</summary>
    public int NoticeYear { get; private set; }

    /// <summary>The job the paper selects for; null for ENEM, entrance exams and certifications (BR5).</summary>
    public string? Position { get; private set; }

    /// <summary>The comparable form of <see cref="Position"/>, empty when there is none (BR10).</summary>
    public string NormalizedPosition { get; private set; } = string.Empty;

    /// <summary>How the notice names itself (BR6).</summary>
    public string? NoticeReference { get; private set; }

    /// <summary>The official address of the notice (BR7).</summary>
    public string? NoticeUrl { get; private set; }

    /// <summary>The day the paper was applied, without a time (BR8).</summary>
    public DateOnly? AppliedOn { get; private set; }

    /// <summary>Draft or Published (BR9).</summary>
    public ExamEditionStatus Status { get; private set; }

    public static Result<ExamEdition> Create(
        Guid examId,
        Guid? organizerId,
        int? noticeYear,
        string? position,
        string? noticeReference,
        string? noticeUrl,
        DateOnly? appliedOn,
        ExamEditionStatus status,
        int maxNoticeYear)
    {
        var edition = new ExamEdition { ExamId = examId };
        var applied = edition.Apply(organizerId, noticeYear, position, noticeReference, noticeUrl, appliedOn, status, maxNoticeYear);

        return applied.IsFailure ? Result.Failure<ExamEdition>(applied.Error!) : Result.Success(edition);
    }

    /// <summary>Replaces every field but the exam, which never changes (BR2).</summary>
    public Result Update(
        Guid? organizerId,
        int? noticeYear,
        string? position,
        string? noticeReference,
        string? noticeUrl,
        DateOnly? appliedOn,
        ExamEditionStatus status,
        int maxNoticeYear) =>
        Apply(organizerId, noticeYear, position, noticeReference, noticeUrl, appliedOn, status, maxNoticeYear);

    /// <summary>BR11: a published edition must be set back to Draft before it can leave.</summary>
    public Result CanBeDeleted() =>
        Status == ExamEditionStatus.Published
            ? Result.Failure(new Error(CatalogErrorCodes.ExamEditionPublished, ErrorKind.Conflict))
            : Result.Success();

    private Result Apply(
        Guid? organizerId,
        int? noticeYear,
        string? position,
        string? noticeReference,
        string? noticeUrl,
        DateOnly? appliedOn,
        ExamEditionStatus status,
        int maxNoticeYear)
    {
        if (organizerId is not { } organizer || organizer == Guid.Empty)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionOrganizerRequired, ErrorKind.Validation));
        }

        if (noticeYear is not { } year || year < CatalogLimits.ExamEditionNoticeYearMin || year > maxNoticeYear)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNoticeYearInvalid, ErrorKind.Validation));
        }

        var trimmedPosition = string.IsNullOrWhiteSpace(position) ? null : position.Trim();
        if (trimmedPosition is { Length: > CatalogLimits.ExamEditionPositionMaxLength })
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionPositionTooLong, ErrorKind.Validation));
        }

        var trimmedReference = string.IsNullOrWhiteSpace(noticeReference) ? null : noticeReference.Trim();
        if (trimmedReference is { Length: > CatalogLimits.ExamEditionNoticeReferenceMaxLength })
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNoticeReferenceTooLong, ErrorKind.Validation));
        }

        var trimmedUrl = string.IsNullOrWhiteSpace(noticeUrl) ? null : noticeUrl.Trim();
        if (trimmedUrl is not null && !IsAbsoluteWebAddress(trimmedUrl))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNoticeUrlInvalid, ErrorKind.Validation));
        }

        if (appliedOn is { } day && day < new DateOnly(year, 1, 1))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionAppliedOnBeforeNoticeYear, ErrorKind.Validation));
        }

        if (!Enum.IsDefined(status))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionStatusInvalid, ErrorKind.Validation));
        }

        OrganizerId = organizer;
        NoticeYear = year;
        Position = trimmedPosition;
        NormalizedPosition = CatalogText.Normalize(trimmedPosition);
        NoticeReference = trimmedReference;
        NoticeUrl = trimmedUrl;
        AppliedOn = appliedOn;
        Status = status;

        return Result.Success();
    }

    // BR7: an address the browser can open. A relative path, a mailto: or a 400-character URL is not one.
    private static bool IsAbsoluteWebAddress(string value) =>
        value.Length <= CatalogLimits.ExamEditionNoticeUrlMaxLength
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
