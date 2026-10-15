using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-104, the rules of a multiple-answer question without a database: what a draft and a published question
/// need (BR3, BR4), the shapes that are refused (BR5), that the type never changes (BR2), the key change check
/// over the set of correct options (BR7) and that nothing about scoring is stored (BR11).
/// </summary>
public sealed class MultipleAnswerQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static OptionDraft Option(string text, bool correct) => new(Text(text), correct);

    private static QuestionContent Multiple(
        IReadOnlyList<OptionDraft>? options = null,
        string statement = "<p>Quais são capitais?</p>",
        bool? trueFalseKey = null,
        QuestionType type = QuestionType.MultipleAnswer) =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            null,
            SanitizedHtml.Empty,
            null,
            options ?? [],
            Type: type,
            TrueFalseKey: trueFalseKey);

    private static IReadOnlyList<OptionDraft> Marked(params bool[] correct) =>
        [.. correct.Select((isCorrect, index) => Option($"Opção {index}", isCorrect))];

    private static Question Published(params bool[] correct)
    {
        var question = Question.Create(Multiple(Marked(correct))).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    // AC1, BR1, BR3: a draft with no options is stored with its type, and any marking is accepted.
    [Fact]
    public void Create_MultipleAnswerWithNoOptions_IsADraftOfThatTypeWithNoKey()
    {
        var result = Question.Create(Multiple());

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(QuestionType.MultipleAnswer);
        result.Value.Status.Should().Be(QuestionStatus.Draft);
        result.Value.Options.Should().BeEmpty();
        result.Value.TrueFalseKey.Should().BeNull();
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Create_MultipleAnswerDraft_AcceptsAnyMarking(bool a, bool b, bool c)
    {
        var result = Question.Create(Multiple(Marked(a, b, c)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Options.Count(option => option.IsCorrect).Should().Be(new[] { a, b, c }.Count(x => x));
    }

    // AC2, BR3: a sixth option is refused on a draft too.
    [Fact]
    public void Create_MultipleAnswerWithSixOptions_IsRefusedWithOptionsCount()
    {
        var result = Question.Create(Multiple(Marked(true, true, false, false, false, false)));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
    }

    // AC3, BR4: publishing needs three to five options, each with text.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Publish_MultipleAnswerWithFewerThanThreeOptions_IsRefusedWithMultipleOptionsCount(int count)
    {
        var question = Question.Create(Multiple(Marked([.. Enumerable.Repeat(true, count)]))).Value;

        var result = question.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionMultipleOptionsCount);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Publish_MultipleAnswerWithAnEmptyOptionText_IsRefusedWithOptionTextRequired()
    {
        var question = Question.Create(Multiple(
            [Option("Paris", true), Option("Roma", true), new OptionDraft(SanitizedHtml.Empty, false)])).Value;

        var result = question.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
    }

    // AC4, BR4: two or more correct and at least one wrong; none, one or all is refused.
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Publish_MultipleAnswerWithAnInvalidMarking_IsRefusedWithMultipleKeyInvalid(bool a, bool b, bool c)
    {
        var question = Question.Create(Multiple(Marked(a, b, c))).Value;

        var result = question.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionMultipleKeyInvalid);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, true, true, false)]
    public void Publish_MultipleAnswerWithTwoOrMoreCorrectAndOneWrong_BecomesPublished(params bool[] marks)
    {
        var question = Question.Create(Multiple(Marked(marks))).Value;

        question.Publish().IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
    }

    // AC5, BR4: an edit of a published question runs the same check, and a refusal changes nothing.
    [Fact]
    public void Update_PublishedMultipleAnswer_LeavingOneCorrect_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var question = Published(true, true, false);

        var result = question.Update(Multiple(Marked(true, false, false), statement: "<p>Outro enunciado.</p>"));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionMultipleKeyInvalid);
        question.Status.Should().Be(QuestionStatus.Published);
        question.StatementHtml.Should().Be("<p>Quais são capitais?</p>");
        question.Options.Count(option => option.IsCorrect).Should().Be(2);
    }

    [Fact]
    public void Update_PublishedMultipleAnswer_MarkingAnotherValidSet_StaysPublished()
    {
        var question = Published(true, true, false);

        var result = question.Update(Multiple(Marked(true, false, true)));

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        question.Options.OrderBy(option => option.Position).Select(option => option.IsCorrect)
            .Should().Equal(true, false, true);
    }

    // AC6, BR5: a true/false key sent with this type is refused, never ignored.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_MultipleAnswerWithATrueFalseKey_IsRefusedWithKeyNotAllowed(bool key)
    {
        var result = Question.Create(Multiple(Marked(true, true, false), trueFalseKey: key));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC7, BR2: the type never changes, in either direction.
    [Fact]
    public void Update_WithAnotherType_IsRefusedWithTypeChanged_AndNothingChanges()
    {
        var question = Question.Create(Multiple(Marked(true, true, false))).Value;

        var result = question.Update(Multiple(Marked(true, false, false), type: QuestionType.SingleChoice));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        question.Type.Should().Be(QuestionType.MultipleAnswer);
        question.Options.Count(option => option.IsCorrect).Should().Be(2);
    }

    [Fact]
    public void Update_SingleChoiceWithMultipleAnswerContent_IsRefusedWithTypeChanged()
    {
        var question = Question.Create(Multiple(
            [Option("Paris", true), Option("Lyon", false)], type: QuestionType.SingleChoice)).Value;

        var result = question.Update(Multiple(Marked(true, true, false)));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
    }

    // AC8, BR7: the key change check compares the set of correct positions, not the first one.
    [Fact]
    public void NextVersion_MovingASecondCorrectOption_IsAKeyChangeThatNeedsAReason()
    {
        var question = Published(true, true, false);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Multiple(Marked(true, false, true))).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    [Fact]
    public void NextVersion_MovingASecondCorrectOption_WithAReason_RecordsAKeyChange()
    {
        var question = Published(true, true, false);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Multiple(Marked(true, false, true))).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, "Gabarito retificado.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(2);
        result.Value.AnswerKeyChanged.Should().BeTrue();
        result.Value.Reason.Should().Be("Gabarito retificado.");
    }

    [Fact]
    public void NextVersion_AddingACorrectOption_IsAKeyChange()
    {
        var question = Published(true, true, false, false);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Multiple(Marked(true, true, true, false))).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    // AC9, BR7: a text edit with the same correct set is a version, not a key change; version 1 never is.
    [Fact]
    public void NextVersion_ChangingOnlyTheTextOfAnOption_IsNotAKeyChange()
    {
        var question = Published(true, true, false);
        var first = question.NextVersion(null, null).Value!;
        first.AnswerKeyChanged.Should().BeFalse();
        question.Update(Multiple([Option("Opção 0", true), Option("Opção 1", true), Option("Outro texto", false)]))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(2);
        result.Value.AnswerKeyChanged.Should().BeFalse();
    }

    // AC10, BR8: the rules the types share are the same rules.
    [Fact]
    public void Create_MultipleAnswerWithAnEmptyStatement_IsRefusedWithStatementRequired()
    {
        var result = Question.Create(Multiple(Marked(true, true, false)) with
        {
            Statement = new SanitizedHtml("<p><br></p>", string.Empty)
        });

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatementRequired);
    }

    [Fact]
    public void EnsureDeletable_PublishedMultipleAnswer_IsRefusedWithDeleteRequiresDraft()
    {
        var question = Published(true, true, false);

        var result = question.EnsureDeletable();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
    }

    // BR1, BR11: the answer is only the marks; no count, scoring mode, penalty or score is stored.
    [Fact]
    public void TheEntityAndTheContracts_CarryNoCountOrScoringModeForTheKey()
    {
        var types = new[]
        {
            typeof(Question), typeof(SaveQuestionRequest), typeof(QuestionResponse), typeof(QuestionListItemResponse)
        };
        var names = types.SelectMany(type => type.GetProperties()).Select(property => property.Name).ToList();

        names.Should().NotContain(name =>
            name.Contains("CorrectCount", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ScoringMode", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Partial", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Penalt", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Score", StringComparison.OrdinalIgnoreCase));
    }
}
