using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.QuestionBank.Contracts;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 BR5 on the checker alone, with no host: the rules the routes tests reach only through a whole attempt, plus the
/// edges they leave out (a repeated item, a minimum of lines that is never checked, line breaks counted once).
/// </summary>
public sealed class AnswerCheckerTests
{
    private static readonly AnswerShape Shape = new(AttemptQuestionType.Matching, [0, 1, 2], [0, 1, 2], [1, 2], null);

    private static AnswerShape Of(AttemptQuestionType type, int? sizeMax = null) =>
        new(type, Shape.OptionPositions, Shape.ItemPositions, Shape.BlankNumbers, sizeMax);

    [Fact]
    public void Matching_AnItemNamedTwice_IsRepeated_ButARightOptionNamedTwiceIsAllowed()
    {
        var repeated = AnswerChecker.Check(Shape, new MatchingPayload([new(0, 1), new(0, 2)]));
        var shared = AnswerChecker.Check(Shape, new MatchingPayload([new(0, 1), new(1, 1)]));

        repeated.Errors.Should().Equal(new AnswerFieldError("pairs[1].itemPosition", ExamAttemptErrorCodes.EntryRepeated));
        shared.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FillInTheBlanks_ABlankNumberNamedTwice_IsRepeated()
    {
        var check = AnswerChecker.Check(
            Of(AttemptQuestionType.FillInTheBlanks),
            new FillInTheBlanksPayload([new(1, "a"), new(1, "b")]));

        check.Errors.Should().Equal(new AnswerFieldError("blanks[1].number", ExamAttemptErrorCodes.EntryRepeated));
    }

    [Fact]
    public void MultipleAnswer_AnOptionNamedTwice_IsKeptOnce()
    {
        var check = AnswerChecker.Check(Of(AttemptQuestionType.MultipleAnswer), new MultipleAnswerPayload([2, 0, 2]));

        check.Answer.Should().BeOfType<MultipleAnswerPayload>().Which.OptionPositions.Should().Equal(2, 0);
    }

    [Fact]
    public void Essay_TheMinimumOfLinesIsNeverChecked_AndALineBreakCountsOnceWhateverItsKind()
    {
        var shape = Of(AttemptQuestionType.Essay, sizeMax: 2);

        AnswerChecker.Check(shape, new EssayPayload("uma")).IsValid.Should().BeTrue("a short essay is judged at grading");
        AnswerChecker.Check(shape, new EssayPayload("uma\r\nduas")).IsValid.Should().BeTrue("CRLF is one line break");
        AnswerChecker.Check(shape, new EssayPayload("uma\rduas\rtrês")).Errors
            .Should().Equal(new AnswerFieldError("text", ExamAttemptErrorCodes.TextTooLong));
    }

    [Fact]
    public void OpenAnswer_CountsCharactersAfterTheLineBreaksAreNormalized()
    {
        var shape = Of(AttemptQuestionType.OpenAnswer, sizeMax: 5);

        AnswerChecker.Check(shape, new OpenAnswerPayload("ab\r\ncd")).IsValid.Should().BeTrue("five characters once CRLF is one");
        AnswerChecker.Check(shape, new OpenAnswerPayload("ab\r\ncde")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AText_IsKeptAsWritten_WithNoTrimAndNoChangeOfCase()
    {
        var check = AnswerChecker.Check(Of(AttemptQuestionType.ShortAnswer), new ShortAnswerPayload("  Brasília \t"));

        check.Answer.Should().Be(new ShortAnswerPayload("  Brasília \t"));
    }

    // BR5: the module's copy of the question types is the question bank's own list, checked over the values so a ninth type
    // fails here and not at run time.
    [Fact]
    public void AttemptQuestionType_IsTheQuestionBanksListOfTypes_ByName()
    {
        Enum.GetNames<AttemptQuestionType>().Should().Equal(Enum.GetNames<QuestionType>());
        foreach (var type in Enum.GetValues<QuestionType>())
        {
            Enum.TryParse<AttemptQuestionType>(type.ToString(), out _).Should().BeTrue($"{type} must exist in the module's copy");
        }
    }
}
