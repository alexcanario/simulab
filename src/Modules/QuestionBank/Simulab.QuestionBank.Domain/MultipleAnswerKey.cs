using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a multiple-answer question (F-104, BR1, BR3 to BR5): the set of options marked correct. A draft
/// may hold any marking; a published question has three to five options, at least two correct and at least one
/// wrong. A true/false key written to it is refused. How many are correct is the count of the marks, never stored.
/// </summary>
/// <param name="Written">The options as written.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal (F-106, BR6).</param>
public sealed record MultipleAnswerKey(
    IReadOnlyList<OptionDraft> Written,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null) : AnswerKey
{
    public override QuestionType Type => QuestionType.MultipleAnswer;

    public override IReadOnlyList<OptionDraft> Options => Written;

    public override bool? TrueFalseValue => null;

    public override OptionFault? Check(bool published)
    {
        if (TrueFalseWritten is not null)
        {
            return new OptionFault(new Error(QuestionBankErrorCodes.QuestionKeyNotAllowed, ErrorKind.Validation), null);
        }

        return MatchItemsRefused(MatchItemsWritten) ?? QuestionRules.CheckMultipleAnswerOptions(Written, published);
    }
}
