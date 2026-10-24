using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a short-answer question (F-108, BR1, BR3 to BR7): exactly one blank, numbered 1 and with no marker in the
/// statement, holding the answers the student may type. A draft may have none yet; a published question has at least
/// one. Options, a true/false key and left items are refused. The limits and the comparison are those of
/// <see cref="FillInTheBlanksKey"/>.
/// </summary>
/// <param name="Written">The blanks as written: none, or one numbered 1.</param>
/// <param name="OptionsWritten">Options that came with them; any of them is a refusal.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal.</param>
public sealed record ShortAnswerKey(
    IReadOnlyList<BlankDraft> Written,
    IReadOnlyList<OptionDraft>? OptionsWritten = null,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null) : AnswerKey
{
    /// <summary>The number of the one blank of the type.</summary>
    public const int BlankNumber = 1;

    public override QuestionType Type => QuestionType.ShortAnswer;

    public override IReadOnlyList<OptionDraft> Options => [];

    public override bool? TrueFalseValue => null;

    // BR3: the response always carries the one blank, with its answers trimmed; none written is a blank with no answer.
    public override IReadOnlyList<BlankDraft> Blanks =>
    [
        new BlankDraft(
            BlankNumber,
            [.. Written.FirstOrDefault(blank => blank.Number == BlankNumber)?.AcceptedAnswers.Select(answer => answer?.Trim() ?? string.Empty) ?? []])
    ];

    public override OptionFault? Check(bool published)
    {
        // BR7: nothing another type has is ignored.
        if (OptionsWritten is { Count: > 0 })
        {
            return FillInTheBlanksKey.Fault(QuestionBankErrorCodes.QuestionOptionsNotAllowed, null);
        }

        if (TrueFalseWritten is not null)
        {
            return FillInTheBlanksKey.Fault(QuestionBankErrorCodes.QuestionKeyNotAllowed, null);
        }

        if (MatchItemsRefused(MatchItemsWritten) is { } refused)
        {
            return refused;
        }

        // BR3: more than one entry, or one that is not number 1, is refused; nothing is ignored.
        if (Written.Count > 1)
        {
            return FillInTheBlanksKey.Fault(QuestionBankErrorCodes.QuestionBlanksCount, null);
        }

        if (Written.Any(blank => blank.Number != BlankNumber))
        {
            return FillInTheBlanksKey.Fault(QuestionBankErrorCodes.QuestionBlankMarkerMissing, null);
        }

        // BR4: broken data is refused on a draft too, with the blank's number as the position.
        var blank = Blanks[0];
        if (FillInTheBlanksKey.CheckAnswers(blank) is { } fault)
        {
            return fault;
        }

        // BR5: to publish, at least one accepted answer.
        return published && blank.AcceptedAnswers.Count == 0
            ? FillInTheBlanksKey.Fault(QuestionBankErrorCodes.QuestionBlankAnswersRequired, blank.Number)
            : null;
    }
}
