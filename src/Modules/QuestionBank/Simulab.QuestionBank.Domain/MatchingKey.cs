using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a matching question (F-106, BR1, BR3 to BR5): the left items, each naming the option (right item) it
/// matches, one to one. The options are the right column and carry no mark; extra options match nothing. A draft
/// may leave a match unset; a published question has 2 to 8 left items, at least as many options, every item
/// matched and every text written. A true/false key or a marked option is refused.
/// </summary>
/// <param name="Written">The options (right items) as written.</param>
/// <param name="Items">The left items as written.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="BlanksWritten">Blanks that came with them; any of them is a refusal (F-107, BR7).</param>
/// <param name="RubricWritten">A rubric that came with them; any criterion is a refusal (F-109, BR8).</param>
/// <param name="SizeWritten">True when a size limit came with them; always a refusal (F-109, BR8).</param>
public sealed record MatchingKey(
    IReadOnlyList<OptionDraft> Written,
    IReadOnlyList<MatchItemDraft> Items,
    bool? TrueFalseWritten = null,
    IReadOnlyList<BlankDraft>? BlanksWritten = null,
    IReadOnlyList<CriterionDraft>? RubricWritten = null,
    bool SizeWritten = false) : AnswerKey
{
    public override QuestionType Type => QuestionType.Matching;

    public override IReadOnlyList<OptionDraft> Options => Written;

    public override IReadOnlyList<MatchItemDraft> MatchItems => Items;

    public override bool? TrueFalseValue => null;

    public override OptionFault? Check(bool published)
    {
        // BR5: the options carry no mark here, and a true/false key belongs to another type; nothing is ignored.
        if (TrueFalseWritten is not null || Written.Any(option => option.IsCorrect))
        {
            return new OptionFault(new Error(QuestionBankErrorCodes.QuestionKeyNotAllowed, ErrorKind.Validation), null);
        }

        return BlanksRefused(BlanksWritten)
            ?? RubricAndSizeRefused(RubricWritten, SizeWritten)
            ?? QuestionRules.CheckMatching(Written, Items, published);
    }
}
