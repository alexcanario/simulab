using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-95, the rules of one question without a database: what a draft needs (BR3), what a published question
/// needs (BR4), the limits (BR9), and that a refused edit leaves the stored question as it was (AC7). The
/// Domain receives sanitized text, so these tests build it by hand.
/// </summary>
public sealed class QuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html, string? plain = null) => new(html, plain ?? html);

    private static OptionDraft Option(string text, bool correct = false) => new(Text(text), correct);

    private static QuestionContent Content(
        Guid? topicId = null,
        string? language = "pt-BR",
        SanitizedHtml? statement = null,
        SanitizedHtml? baseText = null,
        SanitizedHtml? explanation = null,
        int? difficulty = null,
        IReadOnlyList<OptionDraft>? options = null) =>
        new(
            topicId ?? Topic,
            language,
            statement ?? Text("<p>Qual e a capital da França?</p>", "Qual e a capital da França?"),
            baseText ?? SanitizedHtml.Empty,
            explanation ?? SanitizedHtml.Empty,
            difficulty,
            options ?? []);

    private static IReadOnlyList<OptionDraft> TwoOptionsOneKey() => [Option("Paris", correct: true), Option("Lyon")];

    private static Question Published()
    {
        var question = Question.Create(Content(options: TwoOptionsOneKey())).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    // AC1, BR2: a valid question is a single-choice draft with its content.
    [Fact]
    public void Create_ValidContent_IsADraftOfTypeSingleChoice()
    {
        var result = Question.Create(Content(difficulty: 3, options: TwoOptionsOneKey()));

        result.IsSuccess.Should().BeTrue();
        var question = result.Value;
        question.Type.Should().Be(QuestionType.SingleChoice);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.TopicId.Should().Be(Topic);
        question.Difficulty.Should().Be(3);
        question.Options.Select(option => option.Position).Should().Equal(0, 1);
        question.Options.Select(option => option.IsCorrect).Should().Equal(true, false);
    }

    // AC2, BR3: a draft with no options and no key is accepted; an empty statement is not.
    [Fact]
    public void Create_NoOptionsAndNoKey_IsAccepted()
    {
        Question.Create(Content()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_StatementEmptyAfterTags_IsRefused()
    {
        var result = Question.Create(Content(statement: new SanitizedHtml("<p><br></p>", string.Empty)));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatementRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Create_NoTopic_IsRefused()
    {
        var result = Question.Create(Content(topicId: Guid.Empty));

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionTopicNotFound);
    }

    // AC14, BR7: the language is canonicalized and must be supported.
    [Fact]
    public void Create_LanguageInAnyCase_IsStoredCanonical()
    {
        Question.Create(Content(language: "pt-br")).Value.ContentLanguage.Should().Be("pt-BR");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    public void Create_UnsupportedLanguage_IsRefused(string? language)
    {
        var result = Question.Create(Content(language: language));

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionContentLanguageInvalid);
    }

    // AC13, BR6: difficulty is 1 to 5 or empty.
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_DifficultyOutsideTheRange_IsRefused(int difficulty)
    {
        var result = Question.Create(Content(difficulty: difficulty));

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_DifficultyInsideTheRangeOrEmpty_IsAccepted(int? difficulty)
    {
        Question.Create(Content(difficulty: difficulty)).Value.Difficulty.Should().Be(difficulty);
    }

    // AC4, BR3: a sixth option is refused, draft or not.
    [Fact]
    public void Create_SixOptions_IsRefused()
    {
        var options = Enumerable.Range(0, 6).Select(index => Option($"Option {index}")).ToList();

        var result = Question.Create(Content(options: options));

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
    }

    [Fact]
    public void Create_DraftMayHaveAnEmptyOptionText()
    {
        Question.Create(Content(options: [new OptionDraft(SanitizedHtml.Empty, false), Option("B")])).IsSuccess
            .Should().BeTrue();
    }

    // AC15, BR9: over the limit is refused, never truncated; each field has its own code.
    [Fact]
    public void Create_StatementOverTheLimit_IsRefused()
    {
        var statement = Text(new string('a', QuestionLimits.StatementMaxLength + 1));

        Question.Create(Content(statement: statement)).Error!.Code
            .Should().Be(QuestionBankErrorCodes.QuestionStatementTooLong);
    }

    [Fact]
    public void Create_StatementAtTheLimit_IsAccepted()
    {
        var statement = Text(new string('a', QuestionLimits.StatementMaxLength));

        Question.Create(Content(statement: statement)).Value.StatementHtml.Length
            .Should().Be(QuestionLimits.StatementMaxLength);
    }

    [Fact]
    public void Create_BaseTextOverTheLimit_IsRefused()
    {
        var baseText = Text(new string('a', QuestionLimits.BaseTextMaxLength + 1));

        Question.Create(Content(baseText: baseText)).Error!.Code
            .Should().Be(QuestionBankErrorCodes.QuestionBaseTextTooLong);
    }

    [Fact]
    public void Create_ExplanationOverTheLimit_IsRefused()
    {
        var explanation = Text(new string('a', QuestionLimits.ExplanationMaxLength + 1));

        Question.Create(Content(explanation: explanation)).Error!.Code
            .Should().Be(QuestionBankErrorCodes.QuestionExplanationTooLong);
    }

    [Fact]
    public void Create_OptionOverTheLimit_IsRefused()
    {
        var options = new List<OptionDraft> { Option(new string('a', QuestionLimits.OptionMaxLength + 1)) };

        Question.Create(Content(options: options)).Error!.Code
            .Should().Be(QuestionBankErrorCodes.QuestionOptionTooLong);
    }

    // BR10: the plain statement and its comparable form are derived on every write.
    [Fact]
    public void Create_DerivesThePlainAndTheNormalizedStatement()
    {
        var question = Question.Create(Content(statement: Text("<p>Crase é <strong>fundamental</strong></p>", "Crase é fundamental"))).Value;

        question.StatementText.Should().Be("Crase é fundamental");
        question.NormalizedStatement.Should().Be("CRASE E FUNDAMENTAL");
    }

    // AC5, BR4: publishing needs the key; one correct option exactly.
    [Fact]
    public void Publish_CompleteDraft_BecomesPublished_AndKeepsItsContent()
    {
        var question = Question.Create(Content(options: TwoOptionsOneKey())).Value;
        var statement = question.StatementHtml;

        var result = question.Publish();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        question.StatementHtml.Should().Be(statement);
    }

    [Fact]
    public void Publish_OneOption_IsRefusedWithOptionsCount()
    {
        var question = Question.Create(Content(options: [Option("Paris", correct: true)])).Value;

        question.Publish().Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Publish_EmptyOptionText_IsRefusedWithOptionTextRequired()
    {
        var question = Question.Create(Content(options: [new OptionDraft(SanitizedHtml.Empty, true), Option("Lyon")])).Value;

        question.Publish().Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
    }

    [Fact]
    public void Publish_NoCorrectOption_IsRefusedWithKeyInvalid()
    {
        var question = Question.Create(Content(options: [Option("Paris"), Option("Lyon")])).Value;

        question.Publish().Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyInvalid);
    }

    [Fact]
    public void Publish_TwoCorrectOptions_IsRefusedWithKeyInvalid()
    {
        var question = Question.Create(Content(options: [Option("Paris", true), Option("Lyon", true)])).Value;

        question.Publish().Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyInvalid);
    }

    // AC6, BR12: unpublish returns to draft with the same content; both transitions refuse the wrong state.
    [Fact]
    public void Unpublish_PublishedQuestion_ReturnsToDraft_WithTheSameContent()
    {
        var question = Published();
        var options = question.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect)).ToList();

        var result = question.Unpublish();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect))
            .Should().Equal(options);
    }

    [Fact]
    public void Publish_AlreadyPublished_IsAConflict()
    {
        var result = Published().Publish();

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionAlreadyPublished);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public void Unpublish_Draft_IsAConflict()
    {
        var result = Question.Create(Content()).Value.Unpublish();

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionNotPublished);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
    }

    // AC7, BR4: a published question is edited in place and stays published; an edit that breaks it is
    // refused and leaves every field as it was.
    [Fact]
    public void Update_Published_KeepsStatus_AndTakesTheNewContent()
    {
        var question = Published();

        var result = question.Update(Content(
            statement: Text("<p>Nova pergunta</p>", "Nova pergunta"),
            options: [Option("Paris", true), Option("Lyon"), Option("Nice")]));

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        question.StatementHtml.Should().Be("<p>Nova pergunta</p>");
        question.Options.Should().HaveCount(3);
    }

    [Fact]
    public void Update_Published_LeavingOneOption_IsRefused_AndNothingChanges()
    {
        var question = Published();
        var statement = question.StatementHtml;

        var result = question.Update(Content(
            statement: Text("<p>Outra</p>", "Outra"),
            options: [Option("Paris", true)]));

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
        question.StatementHtml.Should().Be(statement);
        question.Options.Should().HaveCount(2);
        question.Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public void Update_Draft_MayStayIncomplete()
    {
        var question = Question.Create(Content(options: TwoOptionsOneKey())).Value;

        question.Update(Content(options: [Option("Only one")])).IsSuccess.Should().BeTrue();
        question.Options.Should().ContainSingle();
    }

    [Fact]
    public void Update_ReordersOptionsByPosition()
    {
        var question = Question.Create(Content(options: [Option("A"), Option("B", true), Option("C")])).Value;

        question.Update(Content(options: [Option("C"), Option("A"), Option("B", true)]));

        question.Options.OrderBy(option => option.Position).Select(option => option.TextHtml)
            .Should().Equal("C", "A", "B");
        question.Options.Single(option => option.IsCorrect).Position.Should().Be(2);
    }

    // BR11: only a draft is deleted.
    [Fact]
    public void EnsureDeletable_Draft_IsOk_Published_IsRefused()
    {
        Question.Create(Content()).Value.EnsureDeletable().IsSuccess.Should().BeTrue();

        var refused = Published().EnsureDeletable();

        refused.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        refused.Error.Kind.Should().Be(ErrorKind.Conflict);
    }
}
