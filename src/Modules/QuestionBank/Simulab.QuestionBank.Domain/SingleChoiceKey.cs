using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a single-choice question: from two to five options once published, exactly one marked correct
/// (F-95, BR3 and BR4). A true/false key written to it is refused (F-97, BR3).
/// </summary>
/// <param name="Written">The options as written.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal (F-106, BR6).</param>
/// <param name="BlanksWritten">Blanks that came with them; any of them is a refusal (F-107, BR7).</param>
/// <param name="RubricWritten">A rubric that came with them; any criterion is a refusal (F-109, BR8).</param>
/// <param name="SizeWritten">True when a size limit came with them; always a refusal (F-109, BR8).</param>
public sealed record SingleChoiceKey(
    IReadOnlyList<OptionDraft> Written,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null,
    IReadOnlyList<BlankDraft>? BlanksWritten = null,
    IReadOnlyList<CriterionDraft>? RubricWritten = null,
    bool SizeWritten = false) : AnswerKey
{
    public override QuestionType Type => QuestionType.SingleChoice;

    public override IReadOnlyList<OptionDraft> Options => Written;

    public override bool? TrueFalseValue => null;

    public override OptionFault? Check(bool published)
    {
        if (TrueFalseWritten is not null)
        {
            return new OptionFault(new Error(QuestionBankErrorCodes.QuestionKeyNotAllowed, ErrorKind.Validation), null);
        }

        return MatchItemsRefused(MatchItemsWritten)
            ?? BlanksRefused(BlanksWritten)
            ?? RubricAndSizeRefused(RubricWritten, SizeWritten)
            ?? QuestionRules.CheckOptions(Written, published);
    }
}
