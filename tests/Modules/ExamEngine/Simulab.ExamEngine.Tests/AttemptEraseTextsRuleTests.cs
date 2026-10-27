using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-167 BR1-BR4 on the entity, with no database: <see cref="Attempt.EraseTexts"/> clears every text and nothing else,
/// on any status, and a second call changes nothing. The samples are read from the payload's own shapes, so a new answer
/// type with no sample here fails instead of keeping its text in silence.
/// </summary>
public sealed class AttemptEraseTextsRuleTests
{
    private const string Marker = "PERSONAL-TEXT";

    /// <summary>One answer of each shape; the shapes that hold text hold the marker.</summary>
    private static readonly Dictionary<Type, (AnswerPayload Answer, AnswerPayload Erased)> Samples = new()
    {
        [typeof(SingleChoicePayload)] = (new SingleChoicePayload(1), new SingleChoicePayload(1)),
        [typeof(TrueFalsePayload)] = (new TrueFalsePayload(true), new TrueFalsePayload(true)),
        [typeof(MultipleAnswerPayload)] = (new MultipleAnswerPayload([0, 2]), new MultipleAnswerPayload([0, 2])),
        [typeof(MatchingPayload)] = (new MatchingPayload([new(0, 1)]), new MatchingPayload([new(0, 1)])),
        [typeof(FillInTheBlanksPayload)] = (
            new FillInTheBlanksPayload([new(1, Marker), new(2, Marker)]),
            new FillInTheBlanksPayload([new(1, null), new(2, null)])),
        [typeof(ShortAnswerPayload)] = (new ShortAnswerPayload(Marker), new ShortAnswerPayload()),
        [typeof(OpenAnswerPayload)] = (new OpenAnswerPayload(Marker), new OpenAnswerPayload()),
        [typeof(EssayPayload)] = (new EssayPayload(Marker), new EssayPayload())
    };

    private static string Json(object? value) => JsonSerializer.Serialize(value, AppJson.Options);

    private static AttemptQuestionType TypeOf(Type shape) =>
        Enum.Parse<AttemptQuestionType>((string)typeof(AnswerPayload).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Single(attribute => attribute.DerivedType == shape).TypeDiscriminator!);

    /// <summary>An attempt with one question of each shape answered, plus one blank question.</summary>
    private static (Attempt Attempt, IReadOnlyList<(Guid QuestionId, Type Shape)> Rows) Answered(bool submit)
    {
        var shapes = Samples.Keys.ToList();
        var questions = shapes
            .Select((shape, index) => new AttemptQuestion(Guid.CreateVersion7(), index + 1, 1, TypeOf(shape)))
            .Append(new AttemptQuestion(Guid.CreateVersion7(), shapes.Count + 1, 1, AttemptQuestionType.ShortAnswer))
            .ToList();
        var attempt = Attempt.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), null, DateTimeOffset.UnixEpoch, questions);
        for (var index = 0; index < shapes.Count; index++)
        {
            attempt.Save(questions[index].QuestionId, Samples[shapes[index]].Answer).IsSuccess.Should().BeTrue();
        }

        if (submit)
        {
            attempt.Submit(DateTimeOffset.UnixEpoch.AddHours(1));
        }

        return (attempt, [.. shapes.Select((shape, index) => (questions[index].QuestionId, shape))]);
    }

    [Fact]
    public void Samples_CoverEveryShapeOfTheAnswer()
    {
        typeof(AnswerPayload).GetCustomAttributes<JsonDerivedTypeAttribute>().Select(attribute => attribute.DerivedType)
            .Should().BeEquivalentTo(Samples.Keys, "a new shape needs a sample saying whether it carries text");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EraseTexts_ClearsEveryText_KeepsEveryOtherValue_OnAnyStatus(bool submit)
    {
        var (attempt, rows) = Answered(submit);
        var status = attempt.Status;
        var submittedAt = attempt.SubmittedAt;

        attempt.EraseTexts().IsSuccess.Should().BeTrue();

        foreach (var (questionId, shape) in rows)
        {
            Json(attempt.AnswerOf(questionId)!.Answer).Should().Be(Json(Samples[shape].Erased), shape.Name);
        }

        attempt.Answers.Should().Contain(answer => answer.Answer == null, "a blank question stays blank");
        Json(attempt.Answers.Select(answer => answer.Answer)).Should().NotContain(Marker);
        attempt.Status.Should().Be(status);
        attempt.SubmittedAt.Should().Be(submittedAt);
    }

    [Fact]
    public void EraseTexts_Twice_TheSecondChangesNothing()
    {
        var (attempt, _) = Answered(submit: true);
        attempt.EraseTexts();
        var afterFirst = attempt.Answers.Select(answer => answer.Answer).ToList();

        attempt.EraseTexts().IsSuccess.Should().BeTrue();

        attempt.Answers.Select(answer => answer.Answer).Should().Equal(afterFirst, "the same instances: nothing was assigned");
    }
}
