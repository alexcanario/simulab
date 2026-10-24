using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// What an open-answer key and an essay key share (F-109, BR1, BR3, BR5 to BR8): the key is the rubric, an ordered list
/// of criteria, each a name, a description and maximum points, plus an optional size limit. A draft may have no criterion
/// yet; a published question has at least one. Options, a true/false key, left items and blanks are refused. The two
/// types differ only in their <see cref="AnswerKey.Type"/> and in how the size limit is read.
/// </summary>
/// <param name="CriteriaWritten">The criteria as written; the key trims and checks them.</param>
/// <param name="SizeMinWritten">The minimum of the size limit as written; null when not set.</param>
/// <param name="SizeMaxWritten">The maximum of the size limit as written; null when not set.</param>
/// <param name="OptionsWritten">Options that came with them; any of them is a refusal.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal.</param>
/// <param name="BlanksWritten">Blanks that came with them; any of them is a refusal.</param>
public abstract record RubricKey(
    IReadOnlyList<CriterionDraft> CriteriaWritten,
    int? SizeMinWritten,
    int? SizeMaxWritten,
    IReadOnlyList<OptionDraft>? OptionsWritten,
    bool? TrueFalseWritten,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten,
    IReadOnlyList<BlankDraft>? BlanksWritten) : AnswerKey
{
    public override IReadOnlyList<OptionDraft> Options => [];

    public override bool? TrueFalseValue => null;

    // BR3: the response always carries the criteria trimmed and in the order written; an empty description is none.
    public override IReadOnlyList<CriterionDraft> Rubric =>
    [
        .. CriteriaWritten.Select(criterion => new CriterionDraft(
            criterion.Name?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(criterion.Description) ? null : criterion.Description.Trim(),
            criterion.MaxPoints))
    ];

    public override int? AnswerSizeMin => SizeMinWritten;

    public override int? AnswerSizeMax => SizeMaxWritten;

    /// <summary>BR7: true when the size limit as written is acceptable for this type; an unset limit always is.</summary>
    protected abstract bool SizeLimitIsValid();

    public override OptionFault? Check(bool published)
    {
        // BR8: nothing another type has is ignored.
        if (OptionsWritten is { Count: > 0 })
        {
            return Fault(QuestionBankErrorCodes.QuestionOptionsNotAllowed, null);
        }

        if (TrueFalseWritten is not null)
        {
            return Fault(QuestionBankErrorCodes.QuestionKeyNotAllowed, null);
        }

        if ((MatchItemsRefused(MatchItemsWritten) ?? BlanksRefused(BlanksWritten)) is { } refused)
        {
            return refused;
        }

        // BR5: broken data is refused on a draft too, with the criterion's position, from 1.
        var criteria = Rubric;
        if (criteria.Count > QuestionLimits.RubricCriteriaMax)
        {
            return Fault(QuestionBankErrorCodes.QuestionRubricCriteriaCount, null);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < criteria.Count; index++)
        {
            if (CheckCriterion(criteria[index], index + 1, seen) is { } fault)
            {
                return fault;
            }
        }

        if (!SizeLimitIsValid())
        {
            return Fault(QuestionBankErrorCodes.QuestionAnswerSizeInvalid, null);
        }

        // BR6: to publish, at least one criterion.
        return published && criteria.Count == 0 ? Fault(QuestionBankErrorCodes.QuestionRubricCriteriaRequired, null) : null;
    }

    // The limits of one criterion; a refusal names it by its position, from 1. The comparison of names is the rule of
    // typed answers: case and extra white space are ignored, accents count (architect #6).
    private static OptionFault? CheckCriterion(CriterionDraft criterion, int position, HashSet<string> seen)
    {
        if (criterion.Name!.Length == 0)
        {
            return Fault(QuestionBankErrorCodes.QuestionCriterionNameRequired, position);
        }

        if (criterion.Name.Length > QuestionLimits.CriterionNameMaxLength)
        {
            return Fault(QuestionBankErrorCodes.QuestionCriterionNameTooLong, position);
        }

        if (criterion.Description is { Length: > QuestionLimits.CriterionDescriptionMaxLength })
        {
            return Fault(QuestionBankErrorCodes.QuestionCriterionDescriptionTooLong, position);
        }

        if (criterion.MaxPoints is not { } points || points < QuestionLimits.CriterionPointsMin || points > QuestionLimits.CriterionPointsMax)
        {
            return Fault(QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange, position);
        }

        return seen.Add(AcceptedAnswer.Normalize(criterion.Name)) ? null : Fault(QuestionBankErrorCodes.QuestionCriterionNameRepeated, position);
    }

    private static OptionFault Fault(string code, int? position) =>
        new(new Error(code, ErrorKind.Validation), position);
}
