using System.Globalization;
using Simulab.ExamEngine.Contracts;

namespace Simulab.Web.Components.Pages.Attempts;

/// <summary>
/// How the simulator reads and writes the answer to an option question (F-169, BR6): the selection a row group shows, the
/// body a save sends, and whether an answer counts as answered. A true or false question shows two rows, position 0 for
/// true and 1 for false, which exist only on this screen.
/// </summary>
internal static class AttemptAnswers
{
    internal const int TrueRow = 0;

    internal const int FalseRow = 1;

    /// <summary>True when the answer holds something. A type this screen cannot answer counts as blank unless it holds a value.</summary>
    internal static bool IsAnswered(AnswerPayload? answer) =>
        answer switch
        {
            SingleChoicePayload single => single.OptionPosition is not null,
            TrueFalsePayload trueFalse => trueFalse.Value is not null,
            MultipleAnswerPayload multiple => multiple.OptionPositions is { Count: > 0 },
            MatchingPayload matching => matching.Pairs is { Count: > 0 },
            FillInTheBlanksPayload blanks => blanks.Blanks?.Any(blank => !string.IsNullOrWhiteSpace(blank.Text)) == true,
            ShortAnswerPayload shortAnswer => !string.IsNullOrWhiteSpace(shortAnswer.Text),
            OpenAnswerPayload open => !string.IsNullOrWhiteSpace(open.Text),
            EssayPayload essay => !string.IsNullOrWhiteSpace(essay.Text),
            _ => false
        };

    /// <summary>The positions that are marked for a stored answer; empty for a blank one.</summary>
    internal static IReadOnlyList<int> Selection(AnswerPayload? answer) =>
        answer switch
        {
            SingleChoicePayload { OptionPosition: { } position } => [position],
            TrueFalsePayload { Value: { } value } => [value ? TrueRow : FalseRow],
            MultipleAnswerPayload { OptionPositions: { } positions } => [.. positions.Order()],
            _ => []
        };

    /// <summary>The body a save sends for a non-empty selection; an empty one is a clear, which sends nothing.</summary>
    internal static AnswerPayload ToPayload(AttemptQuestionType type, IReadOnlyList<int> selection) =>
        type switch
        {
            AttemptQuestionType.TrueFalse => new TrueFalsePayload(selection[0] == TrueRow),
            AttemptQuestionType.MultipleAnswer => new MultipleAnswerPayload([.. selection]),
            _ => new SingleChoicePayload(selection[0])
        };

    /// <summary>The numbers of the blank questions, in paper order, as the submit dialog lists them.</summary>
    internal static string NumberList(IEnumerable<int> numbers) =>
        string.Join(", ", numbers.Select(number => number.ToString(CultureInfo.CurrentCulture)));
}
