using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-97, the rules of a true/false question without a database: the key it stores (BR1), what a draft and a
/// published question need (BR4), the shapes that are refused (BR3), that the type never changes (BR2) and that
/// a refused edit leaves the stored question as it was.
/// </summary>
public sealed class TrueFalseQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static QuestionContent TrueFalse(
        bool? key = null,
        IReadOnlyList<OptionDraft>? options = null,
        SanitizedHtml? statement = null,
        int? difficulty = null,
        QuestionType type = QuestionType.TrueFalse) =>
        new(
            Topic,
            "pt-BR",
            statement ?? Text("<p>A capital da França é Paris.</p>"),
            null,
            SanitizedHtml.Empty,
            difficulty,
            options ?? [],
            Type: type,
            TrueFalseKey: key);

    private static QuestionContent SingleChoice(bool? key = null) =>
        new(
            Topic,
            "pt-BR",
            Text("<p>Qual é a capital da França?</p>"),
            null,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("Paris"), true), new OptionDraft(Text("Lyon"), false)],
            TrueFalseKey: key);

    private static Question PublishedTrueFalse(bool key = true)
    {
        var question = Question.Create(TrueFalse(key)).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    // AC1, BR1: a true/false draft has its type, no options and no key until it is set.
    [Fact]
    public void Create_TrueFalseWithNoKey_IsADraftWithNoOptionsAndNoKey()
    {
        var result = Question.Create(TrueFalse());

        result.IsSuccess.Should().BeTrue();
        var question = result.Value;
        question.Type.Should().Be(QuestionType.TrueFalse);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.TrueFalseKey.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_TrueFalseWithAKey_StoresTheKey(bool key)
    {
        var question = Question.Create(TrueFalse(key)).Value;

        question.TrueFalseKey.Should().Be(key);
    }

    // AC5, BR2: content that names no type is a single-choice question, as before F-97.
    [Fact]
    public void Create_ContentWithNoType_IsSingleChoiceWithNoKey()
    {
        var question = Question.Create(SingleChoice()).Value;

        question.Type.Should().Be(QuestionType.SingleChoice);
        question.TrueFalseKey.Should().BeNull();
        question.Options.Should().HaveCount(2);
    }

    // AC2, BR3: options on a true/false question and a key on a single-choice one are refused, never ignored.
    [Fact]
    public void Create_TrueFalseWithOptions_IsRefusedWithOptionsNotAllowed()
    {
        var result = Question.Create(TrueFalse(options: [new OptionDraft(Text("Paris"), true)]));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_SingleChoiceWithAKey_IsRefusedWithKeyNotAllowed(bool key)
    {
        var result = Question.Create(SingleChoice(key));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC3, BR4: a draft may lack the key; publishing needs it.
    [Fact]
    public void Publish_TrueFalseWithNoKey_IsRefusedWithTrueFalseKeyRequired()
    {
        var question = Question.Create(TrueFalse()).Value;

        var result = question.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTrueFalseKeyRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Publish_TrueFalseWithAKey_BecomesPublished(bool key)
    {
        var question = Question.Create(TrueFalse(key)).Value;

        question.Publish().IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
        question.TrueFalseKey.Should().Be(key);
    }

    // AC4, BR4: a published true/false question may change its key but not lose it, and a refusal changes nothing.
    [Fact]
    public void Update_PublishedTrueFalse_ChangingTheKey_StaysPublishedWithTheNewKey()
    {
        var question = PublishedTrueFalse(key: true);

        var result = question.Update(TrueFalse(false));

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        question.TrueFalseKey.Should().BeFalse();
    }

    [Fact]
    public void Update_PublishedTrueFalse_ClearingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var question = PublishedTrueFalse(key: true);

        var result = question.Update(TrueFalse(key: null, statement: Text("<p>Outro enunciado.</p>")));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTrueFalseKeyRequired);
        question.TrueFalseKey.Should().BeTrue();
        question.StatementHtml.Should().Be("<p>A capital da França é Paris.</p>");
        question.Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public void Update_TrueFalseDraft_ClearingTheKey_IsAccepted()
    {
        var question = Question.Create(TrueFalse(true)).Value;

        question.Update(TrueFalse(null)).IsSuccess.Should().BeTrue();

        question.TrueFalseKey.Should().BeNull();
    }

    // AC6, BR2: the type never changes; the entity is the authority behind the handler's early answer.
    [Fact]
    public void Update_WithAnotherType_IsRefusedWithTypeChanged_AndNothingChanges()
    {
        var question = Question.Create(TrueFalse(true)).Value;

        var result = question.Update(SingleChoice());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        question.Type.Should().Be(QuestionType.TrueFalse);
        question.TrueFalseKey.Should().BeTrue();
        question.Options.Should().BeEmpty();
    }

    [Fact]
    public void Update_SingleChoiceWithATrueFalseContent_IsRefusedWithTypeChanged()
    {
        var question = Question.Create(SingleChoice()).Value;

        var result = question.Update(TrueFalse(true));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        question.Options.Should().HaveCount(2);
        question.TrueFalseKey.Should().BeNull();
    }

    // AC7, BR5: the rules both types share are the same rules.
    [Fact]
    public void Create_TrueFalseWithAnEmptyStatement_IsRefusedWithStatementRequired()
    {
        var result = Question.Create(TrueFalse(statement: new SanitizedHtml("<p><br></p>", string.Empty)));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatementRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_TrueFalseWithDifficultyOutOfRange_IsRefused(int difficulty)
    {
        var result = Question.Create(TrueFalse(difficulty: difficulty));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
    }

    [Fact]
    public void Create_TrueFalseWithAStatementOverTheLimit_IsRefused()
    {
        var statement = new string('a', QuestionLimits.StatementMaxLength + 1);

        var result = Question.Create(TrueFalse(statement: new SanitizedHtml(statement, statement)));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatementTooLong);
    }

    [Fact]
    public void EnsureDeletable_PublishedTrueFalse_IsRefusedWithDeleteRequiresDraft()
    {
        var question = PublishedTrueFalse();

        var result = question.EnsureDeletable();

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
    }

    // AC8, BR1: the key is the only thing a true/false question stores about its answer. No penalty, weight or
    // score exists on the entity or on the contract (the scoring rule belongs to the edition, epic E-6).
    [Fact]
    public void TheEntityAndTheContracts_CarryNoPenaltyWeightOrScore()
    {
        var types = new[]
        {
            typeof(Question), typeof(SaveQuestionRequest), typeof(QuestionResponse), typeof(QuestionListItemResponse)
        };
        var names = types
            .SelectMany(type => type.GetProperties())
            .Select(property => property.Name)
            .ToList();

        names.Should().Contain(["TrueFalseKey"]);
        names.Should().NotContain(name =>
            name.Contains("Penalt", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Weight", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Score", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Fraction", StringComparison.OrdinalIgnoreCase));
    }
}
