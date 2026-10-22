using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a true/false question (F-97, BR1, BR3, BR4): one boolean, true for "certo" and false for
/// "errado", and no options. A draft may have none yet; a published question needs it.
/// </summary>
/// <param name="Written">The options as written; any of them is a refusal.</param>
/// <param name="Value">The key; null when it is not set.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal (F-106, BR6).</param>
/// <param name="BlanksWritten">Blanks that came with them; any of them is a refusal (F-107, BR7).</param>
public sealed record TrueFalseKey(
    IReadOnlyList<OptionDraft> Written,
    bool? Value,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null,
    IReadOnlyList<BlankDraft>? BlanksWritten = null) : AnswerKey
{
    public override QuestionType Type => QuestionType.TrueFalse;

    public override IReadOnlyList<OptionDraft> Options => [];

    public override bool? TrueFalseValue => Value;

    public override OptionFault? Check(bool published)
    {
        if (Written.Count > 0)
        {
            return new OptionFault(new Error(QuestionBankErrorCodes.QuestionOptionsNotAllowed, ErrorKind.Validation), null);
        }

        if ((MatchItemsRefused(MatchItemsWritten) ?? BlanksRefused(BlanksWritten)) is { } refused)
        {
            return refused;
        }

        return published && Value is null
            ? new OptionFault(new Error(QuestionBankErrorCodes.QuestionTrueFalseKeyRequired, ErrorKind.Validation), null)
            : null;
    }
}
