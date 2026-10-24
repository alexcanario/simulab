using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a fill-in-the-blanks question (F-107, BR1, BR3 to BR7): one blank per marker of the statement, in the
/// order the markers appear, each with the answers it accepts. A draft may leave a blank unanswered; a published
/// question has 1 to 10 markers, every one answered. Options, a true/false key and left items are refused.
/// </summary>
/// <param name="Markers">The marker numbers of the statement, in order and with repeats, as the sanitizer read them.</param>
/// <param name="Written">The blanks as written: a number and its accepted answers.</param>
/// <param name="OptionsWritten">Options that came with them; any of them is a refusal.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
/// <param name="MatchItemsWritten">Left items that came with them; any of them is a refusal.</param>
/// <param name="RubricWritten">A rubric that came with them; any criterion is a refusal (F-109, BR8).</param>
/// <param name="SizeWritten">True when a size limit came with them; always a refusal (F-109, BR8).</param>
public sealed record FillInTheBlanksKey(
    IReadOnlyList<int> Markers,
    IReadOnlyList<BlankDraft> Written,
    IReadOnlyList<OptionDraft>? OptionsWritten = null,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null,
    IReadOnlyList<CriterionDraft>? RubricWritten = null,
    bool SizeWritten = false) : AnswerKey
{
    public override QuestionType Type => QuestionType.FillInTheBlanks;

    public override IReadOnlyList<OptionDraft> Options => [];

    public override bool? TrueFalseValue => null;

    // BR3, BR4: one blank per distinct marker, in statement order, its answers trimmed; a marker nothing was written for
    // is a blank with no answer yet.
    public override IReadOnlyList<BlankDraft> Blanks =>
    [
        .. Markers.Distinct().Select(number => new BlankDraft(
            number,
            [.. Written.FirstOrDefault(blank => blank.Number == number)?.AcceptedAnswers.Select(answer => answer?.Trim() ?? string.Empty) ?? []]))
    ];

    public override OptionFault? Check(bool published)
    {
        // BR7: nothing another type has is ignored.
        if (OptionsWritten is { Count: > 0 })
        {
            return Fault(QuestionBankErrorCodes.QuestionOptionsNotAllowed, null);
        }

        if (TrueFalseWritten is not null)
        {
            return Fault(QuestionBankErrorCodes.QuestionKeyNotAllowed, null);
        }

        if ((MatchItemsRefused(MatchItemsWritten) ?? RubricAndSizeRefused(RubricWritten, SizeWritten)) is { } refused)
        {
            return refused;
        }

        // BR4: broken data is refused on a draft too.
        if (Markers.Count > QuestionLimits.BlanksMax)
        {
            return Fault(QuestionBankErrorCodes.QuestionBlanksCount, null);
        }

        if (Markers.Distinct().Count() != Markers.Count || Written.Select(blank => blank.Number).Distinct().Count() != Written.Count)
        {
            return Fault(QuestionBankErrorCodes.QuestionBlankRepeated, null);
        }

        if (Written.Any(blank => !Markers.Contains(blank.Number)))
        {
            return Fault(QuestionBankErrorCodes.QuestionBlankMarkerMissing, null);
        }

        foreach (var blank in Blanks)
        {
            if (CheckAnswers(blank) is { } fault)
            {
                return fault;
            }
        }

        // BR5: to publish, 1 to 10 markers, each with an accepted answer.
        if (!published)
        {
            return null;
        }

        if (Markers.Count == 0)
        {
            return Fault(QuestionBankErrorCodes.QuestionBlanksCount, null);
        }

        var unanswered = Blanks.FirstOrDefault(blank => blank.AcceptedAnswers.Count == 0);

        return unanswered is null ? null : Fault(QuestionBankErrorCodes.QuestionBlankAnswersRequired, unanswered.Number);
    }

    // The limits of one blank; a refusal names the blank by its number (architect #4). F-108: the short-answer key
    // checks its one blank with the same limits.
    internal static OptionFault? CheckAnswers(BlankDraft blank)
    {
        var answers = blank.AcceptedAnswers;
        if (answers.Count > QuestionLimits.BlankAnswersMax)
        {
            return Fault(QuestionBankErrorCodes.QuestionBlankAnswersCount, blank.Number);
        }

        if (answers.Any(answer => answer.Length == 0))
        {
            return Fault(QuestionBankErrorCodes.QuestionBlankAnswerRequired, blank.Number);
        }

        if (answers.Any(answer => answer.Length > QuestionLimits.BlankAnswerMaxLength))
        {
            return Fault(QuestionBankErrorCodes.QuestionBlankAnswerTooLong, blank.Number);
        }

        return answers.Select(AcceptedAnswer.Normalize).Distinct(StringComparer.Ordinal).Count() == answers.Count
            ? null
            : Fault(QuestionBankErrorCodes.QuestionBlankAnswerRepeated, blank.Number);
    }

    internal static OptionFault Fault(string code, int? position) =>
        new(new Error(code, ErrorKind.Validation), position);
}
