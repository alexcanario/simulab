using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// A subject exactly as one edition's notice names and groups it, with the number of questions the notice
/// states (F-74, ADR-0001 #44). It belongs to one edition, fixed at creation (BR1), and is global data:
/// <see cref="TenantEntity.TenantId"/> is null in v1.
/// <para>
/// The entity owns the shape of its fields. Uniqueness inside the edition and the order of the rows need the
/// siblings, so the handler checks the first and <see cref="NoticeSubjectOrder"/> owns the second.
/// </para>
/// </summary>
public sealed class NoticeSubject : TenantEntity
{
    private NoticeSubject()
    {
    }

    /// <summary>The edition whose notice names this subject; fixed at creation (BR1).</summary>
    public Guid ExamEditionId { get; private set; }

    /// <summary>How the notice groups the subject, as typed; null when it does not (BR3).</summary>
    public string? Group { get; private set; }

    /// <summary>The comparable form of <see cref="Group"/>, empty when there is none: "no group" is one group (BR5).</summary>
    public string NormalizedGroup { get; private set; } = string.Empty;

    /// <summary>The subject as the notice names it (BR2).</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Label"/>: what the unique index reads (BR5).</summary>
    public string NormalizedLabel { get; private set; } = string.Empty;

    /// <summary>How many questions the notice states for it; null when it does not say (BR4).</summary>
    public int? QuestionCount { get; private set; }

    /// <summary>Where the row sits inside its edition; groups are blocks of consecutive rows (BR6).</summary>
    public int DisplayOrder { get; private set; }

    public static Result<NoticeSubject> Create(Guid examEditionId, string? group, string? label, int? questionCount)
    {
        var subject = new NoticeSubject { ExamEditionId = examEditionId };
        var applied = subject.Apply(group, label, questionCount);

        return applied.IsFailure
            ? Result.Failure<NoticeSubject>(applied.Error!)
            : Result.Success(subject);
    }

    /// <summary>Replaces the group, the label and the number of questions; the edition never changes (BR1).</summary>
    public Result Update(string? group, string? label, int? questionCount) => Apply(group, label, questionCount);

    internal void PlaceAt(int displayOrder) => DisplayOrder = displayOrder;

    private Result Apply(string? group, string? label, int? questionCount)
    {
        var trimmedLabel = label?.Trim() ?? string.Empty;
        if (trimmedLabel.Length == 0)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectLabelRequired, ErrorKind.Validation));
        }

        if (trimmedLabel.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectLabelTooShort, ErrorKind.Validation));
        }

        if (trimmedLabel.Length > CatalogLimits.NoticeSubjectLabelMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectLabelTooLong, ErrorKind.Validation));
        }

        var trimmedGroup = string.IsNullOrWhiteSpace(group) ? null : group.Trim();
        if (trimmedGroup is { Length: > CatalogLimits.NoticeSubjectGroupMaxLength })
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectGroupTooLong, ErrorKind.Validation));
        }

        if (questionCount is { } count
            && (count < CatalogLimits.NoticeSubjectQuestionCountMin || count > CatalogLimits.NoticeSubjectQuestionCountMax))
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectQuestionCountInvalid, ErrorKind.Validation));
        }

        Group = trimmedGroup;
        NormalizedGroup = CatalogText.Normalize(trimmedGroup);
        Label = trimmedLabel;
        NormalizedLabel = CatalogText.Normalize(trimmedLabel);
        QuestionCount = questionCount;

        return Result.Success();
    }
}
