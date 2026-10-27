using Simulab.ExamEngine.Contracts;
using Simulab.Web.Components.Pages.Attempts;

namespace Simulab.Web.Tests.Attempts;

/// <summary>F-169 BR6: how the simulator reads the answer to an option question and builds the body of a save.</summary>
public sealed class AttemptAnswersTests
{
    [Fact]
    public void Selection_OfEachShape_IsThePositionsThatAreMarked()
    {
        AttemptAnswers.Selection(new SingleChoicePayload(2)).Should().Equal(2);
        AttemptAnswers.Selection(new TrueFalsePayload(true)).Should().Equal(AttemptAnswers.TrueRow);
        AttemptAnswers.Selection(new TrueFalsePayload(false)).Should().Equal(AttemptAnswers.FalseRow);
        AttemptAnswers.Selection(new MultipleAnswerPayload([3, 1])).Should().Equal(1, 3);
    }

    [Fact]
    public void Selection_OfABlankAnswer_IsEmpty()
    {
        AttemptAnswers.Selection(null).Should().BeEmpty();
        AttemptAnswers.Selection(new SingleChoicePayload()).Should().BeEmpty();
        AttemptAnswers.Selection(new TrueFalsePayload()).Should().BeEmpty();
        AttemptAnswers.Selection(new MultipleAnswerPayload()).Should().BeEmpty();
    }

    [Fact]
    public void IsAnswered_IsTrueOnlyWhenTheAnswerHoldsSomething()
    {
        AttemptAnswers.IsAnswered(null).Should().BeFalse();
        AttemptAnswers.IsAnswered(new SingleChoicePayload()).Should().BeFalse();
        AttemptAnswers.IsAnswered(new SingleChoicePayload(0)).Should().BeTrue();
        AttemptAnswers.IsAnswered(new TrueFalsePayload(false)).Should().BeTrue("false is an answer");
        AttemptAnswers.IsAnswered(new MultipleAnswerPayload([])).Should().BeFalse();
        AttemptAnswers.IsAnswered(new MultipleAnswerPayload([0])).Should().BeTrue();
        AttemptAnswers.IsAnswered(new ShortAnswerPayload("  ")).Should().BeFalse();
        AttemptAnswers.IsAnswered(new MatchingPayload([new MatchingPairPayload(0, 1)])).Should().BeTrue();
    }

    [Fact]
    public void ToPayload_BuildsTheShapeOfTheQuestionsType()
    {
        AttemptAnswers.ToPayload(AttemptQuestionType.SingleChoice, [1]).Should().Be(new SingleChoicePayload(1));
        AttemptAnswers.ToPayload(AttemptQuestionType.TrueFalse, [AttemptAnswers.FalseRow]).Should().Be(new TrueFalsePayload(false));
        AttemptAnswers.ToPayload(AttemptQuestionType.TrueFalse, [AttemptAnswers.TrueRow]).Should().Be(new TrueFalsePayload(true));
        AttemptAnswers.ToPayload(AttemptQuestionType.MultipleAnswer, [0, 2])
            .Should().BeOfType<MultipleAnswerPayload>().Which.OptionPositions.Should().Equal(0, 2);
    }

    [Fact]
    public void NumberList_JoinsTheNumbersInTheGivenOrder()
    {
        AttemptAnswers.NumberList([12, 30, 58]).Should().Be("12, 30, 58");
    }
}
