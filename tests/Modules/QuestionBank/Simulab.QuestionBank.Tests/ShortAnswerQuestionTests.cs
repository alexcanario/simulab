using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-108, the rules of a short-answer question without a database: the key is one blank numbered 1 with no marker
/// (BR1, BR3), what a draft and a published question need (BR4, BR5), the shapes that are refused (BR3, BR7), the type
/// that never changes (BR2) and the key change check over the blank (BR9). The statement carries no marker for this type.
/// </summary>
public sealed class ShortAnswerQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static BlankDraft Blank(int number, params string[] answers) => new(number, answers);

    private static QuestionContent ShortAnswer(
        IReadOnlyList<BlankDraft>? blanks = null,
        string statement = "<p>Qual é a capital do Brasil?</p>",
        IReadOnlyList<OptionDraft>? options = null,
        bool? trueFalseKey = null,
        IReadOnlyList<MatchItemDraft>? matchItems = null,
        QuestionType type = QuestionType.ShortAnswer) =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            null,
            SanitizedHtml.Empty,
            null,
            options ?? [],
            Type: type,
            TrueFalseKey: trueFalseKey,
            MatchItems: matchItems,
            Blanks: blanks);

    private static Question Published(params string[] answers)
    {
        var question = Question.Create(ShortAnswer([Blank(1, answers.Length == 0 ? ["Brasília"] : answers)])).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    private static IEnumerable<(int Number, IReadOnlyList<string> Answers)> BlanksOf(Question question) =>
        question.Blanks.OrderBy(blank => blank.Position).Select(blank => (blank.Number, blank.AcceptedAnswers));

    private static string? CodeOf(Result result) => result.IsFailure ? result.Error!.Code : null;

    private static string? CodeOf<T>(Result<T> result) => result.IsFailure ? result.Error!.Code : null;

    // AC1, BR1, BR3: with no blanks sent the draft still has its one blank, numbered 1, with no answer.
    [Fact]
    public void Create_WithNoBlanks_IsADraftOfThatType_WithOneEmptyBlankNumbered1()
    {
        var result = Question.Create(ShortAnswer());

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(QuestionType.ShortAnswer);
        result.Value.Status.Should().Be(QuestionStatus.Draft);
        result.Value.Options.Should().BeEmpty();
        result.Value.TrueFalseKey.Should().BeNull();
        BlanksOf(result.Value).Should().ContainSingle().Which.Should().Be((1, (IReadOnlyList<string>)[]));
    }

    // AC2, BR4, BR6: stored trimmed, in the order written; the accent counts.
    [Fact]
    public void Create_WithBrasiliaAndBrasilia_StoresBothTrimmedInOrder()
    {
        var result = Question.Create(ShortAnswer([Blank(1, " Brasília ", "Brasilia")]));

        result.IsSuccess.Should().BeTrue();
        BlanksOf(result.Value).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Brasilia");
    }

    // AC3, BR4, BR6: the limits of the one blank.
    [Fact]
    public void Create_WithSixAnswers_IsRefusedWithBlankAnswersCount_AtPosition1()
    {
        var key = AnswerKey.From(QuestionType.ShortAnswer, [], null, null, [Blank(1, "a", "b", "c", "d", "e", "f")]);

        key.Check(published: false)!.Error.Code.Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersCount);
        key.Check(published: false)!.Position.Should().Be(1);
        CodeOf(Question.Create(ShortAnswer([Blank(1, "a", "b", "c", "d", "e", "f")]))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersCount);
    }

    [Fact]
    public void Create_WithAnAnswerOf101Characters_IsRefusedWithBlankAnswerTooLong_And100IsAccepted()
    {
        var tooLong = new string('a', QuestionLimits.BlankAnswerMaxLength + 1);
        var longest = new string('a', QuestionLimits.BlankAnswerMaxLength);

        CodeOf(Question.Create(ShortAnswer([Blank(1, tooLong)]))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerTooLong);
        Question.Create(ShortAnswer([Blank(1, $"  {longest}  ")])).IsSuccess.Should().BeTrue("the answer is trimmed before it is measured");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithAnEmptyAnswer_IsRefusedWithBlankAnswerRequired(string answer)
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "Brasília", answer)]))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerRequired);
    }

    [Fact]
    public void Create_WithTwoAnswersEqualUnderTheRule_IsRefusedWithBlankAnswerRepeated_AtPosition1()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "Brasília", " brasília  ")]))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerRepeated);
        AnswerKey.From(QuestionType.ShortAnswer, [], null, null, [Blank(1, "Brasília", " brasília  ")])
            .Check(published: false)!.Position.Should().Be(1);
    }

    // AC4, BR3: more than one blank, or one not numbered 1, is refused.
    [Fact]
    public void Create_WithTwoBlanks_IsRefusedWithBlanksCount()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "a"), Blank(2, "b")]))).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
    }

    [Fact]
    public void Create_WithABlankNumbered2_IsRefusedWithBlankMarkerMissing()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(2, "a")]))).Should().Be(QuestionBankErrorCodes.QuestionBlankMarkerMissing);
        Question.Create(ShortAnswer([Blank(1, "a")])).IsSuccess.Should().BeTrue("the same answer numbered 1 is accepted");
    }

    // AC5, BR5
    [Fact]
    public void Publish_WithNoAnswer_IsRefusedWithBlankAnswersRequired_AndWithOne_BecomesPublished()
    {
        var question = Question.Create(ShortAnswer()).Value;

        var refused = question.Publish();

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        refused.Error!.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
        Question.Create(ShortAnswer([Blank(1, "Brasília")])).Value.Publish().IsSuccess.Should().BeTrue();
    }

    // AC6, BR5
    [Fact]
    public void Update_Published_RemovingEveryAnswer_IsRefused_AndNothingChanges()
    {
        var question = Published();

        var result = question.Update(ShortAnswer([Blank(1)], "<p>Outro enunciado</p>"));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        question.StatementHtml.Should().Be("<p>Qual é a capital do Brasil?</p>");
        BlanksOf(question).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
    }

    [Fact]
    public void Update_Published_WithAnotherValidSet_StaysPublished()
    {
        var question = Published();

        question.Update(ShortAnswer([Blank(1, "Brasília", "Capital Federal")])).IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
        BlanksOf(question).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Capital Federal");
    }

    // AC7, BR7: what the type does not have is refused, never ignored.
    [Fact]
    public void Create_WithOptions_IsRefusedWithOptionsNotAllowed()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "x")], options: [new OptionDraft(Text("A"), false)])))
            .Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
    }

    [Fact]
    public void Create_WithATrueFalseKey_IsRefusedWithKeyNotAllowed()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "x")], trueFalseKey: true))).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        Question.Create(ShortAnswer([Blank(1, "x")])).IsSuccess.Should().BeTrue("the same content without the key is accepted");
    }

    [Fact]
    public void Create_WithMatchItems_IsRefusedWithMatchItemsNotAllowed()
    {
        CodeOf(Question.Create(ShortAnswer([Blank(1, "x")], matchItems: [new MatchItemDraft(Text("x"), null)])))
            .Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        Question.Create(ShortAnswer([Blank(1, "x")])).IsSuccess.Should().BeTrue("the same content without the items is accepted");
    }

    // AC8, BR2
    [Fact]
    public void Update_WithContentOfAnotherType_IsAConflict()
    {
        var question = Published();

        var result = question.Update(ShortAnswer(statement: "<p>Mudou</p>", type: QuestionType.SingleChoice));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        result.Error!.Kind.Should().Be(ErrorKind.Conflict);
        question.Type.Should().Be(QuestionType.ShortAnswer);
        BlanksOf(question).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
    }

    // AC9, BR7: [[1]] is plain statement text, no marker is read, so no blank error is raised.
    [Fact]
    public void Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsText_AndStoresOneBlank()
    {
        var result = Question.Create(ShortAnswer([Blank(1, "x")], "<p>Digite [[1]] como está.</p>"));

        result.IsSuccess.Should().BeTrue();
        result.Value.StatementHtml.Should().Contain("[[1]]");
        BlanksOf(result.Value).Should().ContainSingle();
    }

    // AC10, BR9: a new accepted answer is a key change and needs its reason; the version snapshots the blank.
    [Fact]
    public void NextVersion_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChangeWithTheBlank()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        first.Blanks.Should().ContainSingle().Which.Number.Should().Be(1);
        question.Update(ShortAnswer([Blank(1, "Brasília", "Brasilia")])).IsSuccess.Should().BeTrue();

        var refused = question.NextVersion(first, null);
        var recorded = question.NextVersion(first, "Aceita a grafia sem acento.");

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        recorded.Value!.AnswerKeyChanged.Should().BeTrue();
        recorded.Value.Reason.Should().Be("Aceita a grafia sem acento.");
        recorded.Value.Blanks.Single().AcceptedAnswers.Should().Equal("Brasília", "Brasilia");
        first.Blanks.Single().AcceptedAnswers.Should().Equal("Brasília");
    }

    // AC11, BR9: the statement, the order of the answers or only their case are not a key change.
    [Fact]
    public void NextVersion_ChangingOnlyTheStatement_IsAVersionWithNoKeyChange()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(ShortAnswer([Blank(1, "Brasília")], "<p>Qual é a capital federal?</p>")).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AnswerKeyChanged.Should().BeFalse();
    }

    [Fact]
    public void NextVersion_ReorderingAnswersOrChangingOnlyTheirCase_IsAVersionWithNoKeyChange()
    {
        var question = Published("Brasília", "Brasilia");
        var first = question.NextVersion(null, null).Value!;
        question.Update(ShortAnswer([Blank(1, "brasilia", "BRASÍLIA")])).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value.Should().NotBeNull("the stored answers changed, so a version is recorded");
        result.Value!.AnswerKeyChanged.Should().BeFalse();
    }

    [Fact]
    public void NextVersion_NothingChanged_MakesNothing()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;

        question.NextVersion(first, null).Value.Should().BeNull();
    }

    // AC12, BR10: annulment and reinstatement keep the blank of the version they copy.
    [Fact]
    public void AnnulAndReinstate_CopyTheBlankOfTheLatestVersion()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;

        var annulled = question.Annul(first, "Anulada pela banca.").Value!;
        var reinstated = question.Reinstate(annulled, "Recurso indeferido.").Value!;

        annulled.Blanks.Single().AcceptedAnswers.Should().Equal("Brasília");
        reinstated.Blanks.Single().Number.Should().Be(1);
        question.NextVersion(reinstated, null).Value.Should().BeNull("the copy is the same versioned content");
    }
}
