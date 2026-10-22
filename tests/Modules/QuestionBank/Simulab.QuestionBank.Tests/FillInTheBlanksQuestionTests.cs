using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-107, the rules of a fill-in-the-blanks question without a database: what a draft and a published question need
/// (BR3 to BR5), the shapes that are refused (BR7), the type that never changes (BR2), the key change check over the
/// blanks (BR10) and the copy an annulment makes (BR11). The statement's markers come as the sanitizer reads them.
/// </summary>
public sealed class FillInTheBlanksQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private const string CapitalStatement = "<p>A capital do Brasil é [[1]] e foi fundada em [[2]].</p>";

    private static SanitizedHtml Text(string html) => new(html, html);

    private static SanitizedHtml Statement(string html, params int[] markers) => new(html, html) { BlankMarkers = markers };

    private static BlankDraft Blank(int number, params string[] answers) => new(number, answers);

    private static QuestionContent FillIn(
        SanitizedHtml? statement = null,
        IReadOnlyList<BlankDraft>? blanks = null,
        IReadOnlyList<OptionDraft>? options = null,
        bool? trueFalseKey = null,
        IReadOnlyList<MatchItemDraft>? matchItems = null,
        QuestionType type = QuestionType.FillInTheBlanks) =>
        new(
            Topic,
            "pt-BR",
            statement ?? Statement(CapitalStatement, 1, 2),
            null,
            SanitizedHtml.Empty,
            null,
            options ?? [],
            Type: type,
            TrueFalseKey: trueFalseKey,
            MatchItems: matchItems,
            Blanks: blanks);

    private static IReadOnlyList<BlankDraft> CapitalBlanks() => [Blank(1, "Brasília"), Blank(2, "1960")];

    private static Question Published()
    {
        var question = Question.Create(FillIn(blanks: CapitalBlanks())).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    private static IEnumerable<(int Number, IReadOnlyList<string> Answers)> BlanksOf(Question question) =>
        question.Blanks.OrderBy(blank => blank.Position).Select(blank => (blank.Number, blank.AcceptedAnswers));

    private static string? CodeOf(Result result) => result.IsFailure ? result.Error!.Code : null;

    private static string? CodeOf<T>(Result<T> result) => result.IsFailure ? result.Error!.Code : null;

    // AC1, BR1, BR4: a statement with no marker is a valid draft of the type, with no options and no key.
    [Fact]
    public void Create_FillInTheBlanksWithNoMarker_IsADraftOfThatType()
    {
        var result = Question.Create(FillIn(Statement("<p>Complete a frase.</p>")));

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(QuestionType.FillInTheBlanks);
        result.Value.Status.Should().Be(QuestionStatus.Draft);
        result.Value.Blanks.Should().BeEmpty();
        result.Value.Options.Should().BeEmpty();
        result.Value.TrueFalseKey.Should().BeNull();
        CodeOf(result.Value.Publish()).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount, "its key is the blanks of the type");
    }

    // AC2, BR3, BR4: numbers need not be consecutive; a marker with no answer is unfinished work, kept as a blank.
    [Fact]
    public void Create_DraftWithMarkers1And3_AndAnswersForBlank3Only_KeepsBothBlanksInStatementOrder()
    {
        var result = Question.Create(FillIn(
            Statement("<p>[[3]] veio antes de [[1]].</p>", 3, 1),
            [Blank(3, "Pedro Álvares Cabral")]));

        result.IsSuccess.Should().BeTrue();
        var blanks = BlanksOf(result.Value).ToList();
        blanks.Select(blank => blank.Number).Should().Equal(3, 1);
        blanks[0].Answers.Should().Equal("Pedro Álvares Cabral");
        blanks[1].Answers.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithElevenMarkers_IsRefusedWithBlanksCount()
    {
        var markers = Enumerable.Range(1, 11).ToArray();
        var html = $"<p>{string.Join(' ', markers.Select(BlankMarker.For))}</p>";

        CodeOf(Question.Create(FillIn(Statement(html, markers)))).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
    }

    [Fact]
    public void Create_WithTenMarkers_IsAccepted()
    {
        var markers = Enumerable.Range(1, 10).ToArray();
        var html = $"<p>{string.Join(' ', markers.Select(BlankMarker.For))}</p>";

        var result = Question.Create(FillIn(Statement(html, markers)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Blanks.Should().HaveCount(10);
    }

    // AC3, BR4: broken data is refused even on a draft.
    [Fact]
    public void Create_WithTheSameMarkerTwice_IsRefusedWithBlankRepeated()
    {
        CodeOf(Question.Create(FillIn(Statement("<p>[[2]] e [[2]]</p>", 2, 2))))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankRepeated);
    }

    [Fact]
    public void Create_WithABlankForANumberWithNoMarker_IsRefusedWithBlankMarkerMissing()
    {
        CodeOf(Question.Create(FillIn(blanks: [Blank(1, "Brasília"), Blank(4, "x")])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankMarkerMissing);
    }

    [Fact]
    public void Create_WithTheSameNumberTwiceInTheBlanksSent_IsRefusedWithBlankRepeated()
    {
        CodeOf(Question.Create(FillIn(blanks: [Blank(1, "Brasília"), Blank(1, "Rio")])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankRepeated);
    }

    // AC4, BR4, BR6: the limits of each blank.
    [Fact]
    public void Create_WithSixAnswersInABlank_IsRefusedWithBlankAnswersCount()
    {
        CodeOf(Question.Create(FillIn(blanks: [Blank(1, "a", "b", "c", "d", "e", "f")])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersCount);
    }

    [Fact]
    public void Create_WithAnAnswerOf101Characters_IsRefusedWithBlankAnswerTooLong_And100IsAccepted()
    {
        var tooLong = new string('a', QuestionLimits.BlankAnswerMaxLength + 1);
        var longest = new string('a', QuestionLimits.BlankAnswerMaxLength);

        CodeOf(Question.Create(FillIn(blanks: [Blank(1, tooLong)]))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerTooLong);
        var accepted = Question.Create(FillIn(blanks: [Blank(1, $"  {longest}  ")]));
        accepted.IsSuccess.Should().BeTrue("the answer is trimmed before it is measured");
        BlanksOf(accepted.Value).First().Answers.Should().Equal(longest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithAnEmptyAnswer_IsRefusedWithBlankAnswerRequired(string answer)
    {
        CodeOf(Question.Create(FillIn(blanks: [Blank(1, "Brasília", answer)])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerRequired);
    }

    [Fact]
    public void Create_WithTwoAnswersEqualUnderTheRule_IsRefusedWithBlankAnswerRepeated()
    {
        CodeOf(Question.Create(FillIn(blanks: [Blank(1, "Brasília", " brasília  ")])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlankAnswerRepeated);
    }

    [Fact]
    public void Create_WithTheSameAnswerWithAndWithoutAccent_IsAccepted_AndAnswersAreStoredTrimmed()
    {
        var result = Question.Create(FillIn(blanks: [Blank(1, " Brasília ", "Brasilia")]));

        result.IsSuccess.Should().BeTrue();
        BlanksOf(result.Value).First().Answers.Should().Equal("Brasília", "Brasilia");
    }

    // AC5, BR5: one to ten markers, every one answered.
    [Fact]
    public void Publish_WithNoMarker_IsRefusedWithBlanksCount()
    {
        var question = Question.Create(FillIn(Statement("<p>Sem lacuna.</p>"))).Value;

        var result = question.Publish();

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Publish_WithAMarkerWithNoAnswer_IsRefusedWithBlankAnswersRequired()
    {
        var question = Question.Create(FillIn(blanks: [Blank(1, "Brasília")])).Value;

        CodeOf(question.Publish()).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Publish_WithEveryMarkerAnswered_BecomesPublished()
    {
        var question = Question.Create(FillIn(blanks: CapitalBlanks())).Value;

        question.Publish().IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
    }

    // AC6, BR5: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public void Update_Published_RemovingEveryAnswerOfABlank_IsRefused_AndNothingChanges()
    {
        var question = Published();

        var result = question.Update(FillIn(
            Statement("<p>Outro: [[1]] e [[2]].</p>", 1, 2), [Blank(1, "Brasília"), Blank(2)]));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        question.StatementHtml.Should().Be(CapitalStatement);
        BlanksOf(question).Select(blank => blank.Answers.Single()).Should().Equal("Brasília", "1960");
    }

    [Fact]
    public void Update_Published_RemovingEveryMarker_IsRefusedWithBlanksCount()
    {
        var question = Published();

        CodeOf(question.Update(FillIn(Statement("<p>Sem lacunas.</p>")))).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
        question.Blanks.Should().HaveCount(2);
    }

    [Fact]
    public void Update_Published_WithAnotherValidSet_StaysPublished()
    {
        var question = Published();

        question.Update(FillIn(Statement("<p>Fundada em [[2]].</p>", 2), [Blank(2, "1960", "21 de abril de 1960")]))
            .IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
        BlanksOf(question).Should().ContainSingle().Which.Answers.Should().Equal("1960", "21 de abril de 1960");
    }

    // AC7, BR7: what the type does not have is refused, never ignored.
    [Fact]
    public void Create_FillInTheBlanksWithOptions_IsRefusedWithOptionsNotAllowed()
    {
        CodeOf(Question.Create(FillIn(blanks: CapitalBlanks(), options: [new OptionDraft(Text("A"), false)])))
            .Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
    }

    [Fact]
    public void Create_FillInTheBlanksWithATrueFalseKey_IsRefusedWithKeyNotAllowed()
    {
        CodeOf(Question.Create(FillIn(blanks: CapitalBlanks(), trueFalseKey: true)))
            .Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        Question.Create(FillIn(blanks: CapitalBlanks())).Value.Blanks.Should().HaveCount(2, "the same content without the key is accepted");
    }

    [Fact]
    public void Create_FillInTheBlanksWithMatchItems_IsRefusedWithMatchItemsNotAllowed()
    {
        CodeOf(Question.Create(FillIn(blanks: CapitalBlanks(), matchItems: [new MatchItemDraft(Text("x"), null)])))
            .Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        Question.Create(FillIn(blanks: CapitalBlanks())).Value.Blanks.Should().HaveCount(2, "the same content without the items is accepted");
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    [InlineData(QuestionType.MultipleAnswer)]
    [InlineData(QuestionType.Matching)]
    public void Create_AnotherTypeWithBlanks_IsRefusedWithBlanksNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(FillIn(Statement("<p>Enunciado</p>"), [Blank(1, "x")], type: type)))
            .Should().Be(QuestionBankErrorCodes.QuestionBlanksNotAllowed);
    }

    // AC8, BR2: the type never changes.
    [Fact]
    public void Update_WithContentOfAnotherType_IsAConflict()
    {
        var question = Question.Create(FillIn(blanks: CapitalBlanks())).Value;

        var result = question.Update(FillIn(Statement("<p>Mudou</p>"), type: QuestionType.SingleChoice));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        result.Error!.Kind.Should().Be(ErrorKind.Conflict);
        question.Type.Should().Be(QuestionType.FillInTheBlanks);
        question.Blanks.Should().HaveCount(2, "the refused update leaves the blanks as they were");
    }

    // AC10, BR10: a new accepted answer is a key change and needs its reason; the version snapshots the blanks.
    [Fact]
    public void NextVersion_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChangeWithTheBlanks()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        first.Blanks.Select(blank => blank.Number).Should().Equal(1, 2);
        question.Update(FillIn(blanks: [Blank(1, "Brasília", "Brasilia"), Blank(2, "1960")])).IsSuccess.Should().BeTrue();

        var refused = question.NextVersion(first, null);
        var recorded = question.NextVersion(first, "Aceita a grafia sem acento.");

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        recorded.Value!.AnswerKeyChanged.Should().BeTrue();
        recorded.Value.Reason.Should().Be("Aceita a grafia sem acento.");
        recorded.Value.Blanks.Single(blank => blank.Number == 1).AcceptedAnswers.Should().Equal("Brasília", "Brasilia");
        first.Blanks.Single(blank => blank.Number == 1).AcceptedAnswers.Should().Equal("Brasília");
    }

    [Fact]
    public void NextVersion_AddingABlank_NeedsAReason()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(FillIn(
            Statement("<p>[[1]], [[2]] e [[3]].</p>", 1, 2, 3),
            [Blank(1, "Brasília"), Blank(2, "1960"), Blank(3, "JK")])).IsSuccess.Should().BeTrue();

        CodeOf(question.NextVersion(first, null)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    [Fact]
    public void NextVersion_RemovingABlank_NeedsAReason()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(FillIn(Statement("<p>Capital: [[1]].</p>", 1), [Blank(1, "Brasília")])).IsSuccess.Should().BeTrue();

        CodeOf(question.NextVersion(first, null)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    // AC11, BR10: the text around the markers, their order, and the order or case of the answers are not a key change.
    [Fact]
    public void NextVersion_ChangingOnlyTheTextAroundTheMarkers_IsAVersionWithNoKeyChange()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(FillIn(Statement("<p>Capital federal: [[1]]; fundação: [[2]].</p>", 1, 2), CapitalBlanks()))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AnswerKeyChanged.Should().BeFalse();
    }

    [Fact]
    public void NextVersion_SwappingTheMarkers_IsAVersionWithNoKeyChange_InTheNewStatementOrder()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(FillIn(Statement("<p>Fundada em [[2]], a capital é [[1]].</p>", 2, 1), CapitalBlanks()))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value!.AnswerKeyChanged.Should().BeFalse();
        result.Value.Blanks.Select(blank => blank.Number).Should().Equal(2, 1);
    }

    [Fact]
    public void NextVersion_ReorderingAnswersOrChangingOnlyTheirCase_IsAVersionWithNoKeyChange()
    {
        var question = Question.Create(FillIn(blanks: [Blank(1, "Brasília", "Brasilia"), Blank(2, "1960")])).Value;
        question.Publish().IsSuccess.Should().BeTrue();
        var first = question.NextVersion(null, null).Value!;
        question.Update(FillIn(blanks: [Blank(1, "brasilia", "BRASÍLIA"), Blank(2, "1960")])).IsSuccess.Should().BeTrue();

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

    // AC12, BR11: annulment and reinstatement keep the blanks of the version they copy.
    [Fact]
    public void AnnulAndReinstate_CopyTheBlanksOfTheLatestVersion()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;

        var annulled = question.Annul(first, "Anulada pela banca.").Value!;
        var reinstated = question.Reinstate(annulled, "Recurso indeferido.").Value!;

        annulled.Blanks.Select(blank => blank.Number).Should().Equal(1, 2);
        reinstated.Blanks.Single(blank => blank.Number == 2).AcceptedAnswers.Should().Equal("1960");
        question.NextVersion(reinstated, null).Value.Should().BeNull("the copy is the same versioned content");
    }
}
