using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-109, the rules of an open-answer and of an essay question without a database: the rubric is the key (BR1, BR3),
/// what a draft and a published question need (BR5, BR6), the size limit (BR7), what is refused (BR2, BR8), and the
/// key change check over the criteria (BR10). The two types share one rule, so most tests run for both.
/// </summary>
public sealed class RubricQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static CriterionDraft Criterion(string? name, int? points = 10, string? description = null) => new(name, description, points);

    private static QuestionContent Content(
        QuestionType type,
        IReadOnlyList<CriterionDraft>? rubric = null,
        string statement = "<p>Discuta o tema.</p>",
        string? explanation = null,
        int? sizeMin = null,
        int? sizeMax = null,
        IReadOnlyList<OptionDraft>? options = null,
        bool? trueFalseKey = null,
        IReadOnlyList<MatchItemDraft>? matchItems = null,
        IReadOnlyList<BlankDraft>? blanks = null) =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            null,
            explanation is null ? SanitizedHtml.Empty : Text(explanation),
            null,
            options ?? [],
            Type: type,
            TrueFalseKey: trueFalseKey,
            MatchItems: matchItems,
            Blanks: blanks,
            Rubric: rubric,
            AnswerSizeMin: sizeMin,
            AnswerSizeMax: sizeMax);

    private static Question Published(QuestionType type)
    {
        var question = Question.Create(Content(type, [Criterion("Theme", 30), Criterion("Argument", 40)])).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    private static string? CodeOf(Result result) => result.IsFailure ? result.Error!.Code : null;

    private static string? CodeOf<T>(Result<T> result) => result.IsFailure ? result.Error!.Code : null;

    private static OptionFault? CheckOf(QuestionType type, IReadOnlyList<CriterionDraft>? rubric, bool published = false) =>
        AnswerKey.From(type, [], null, null, null, null, rubric).Check(published);

    public static TheoryData<QuestionType> RubricTypes => new() { QuestionType.OpenAnswer, QuestionType.Essay };

    public static TheoryData<QuestionType> OtherTypes => new()
    {
        QuestionType.SingleChoice,
        QuestionType.TrueFalse,
        QuestionType.MultipleAnswer,
        QuestionType.Matching,
        QuestionType.FillInTheBlanks,
        QuestionType.ShortAnswer
    };

    // AC1, BR1, BR5: a draft of that type may have no criterion yet, no option, no key, no item and no blank.
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithNoRubric_IsADraftOfThatType_WithAnEmptyRubricAndNoSize(QuestionType type)
    {
        var result = Question.Create(Content(type));

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(type);
        result.Value.Status.Should().Be(QuestionStatus.Draft);
        result.Value.Rubric.Should().BeEmpty();
        result.Value.AnswerSizeMin.Should().BeNull();
        result.Value.AnswerSizeMax.Should().BeNull();
        result.Value.Options.Should().BeEmpty();
        result.Value.Blanks.Should().BeEmpty();
        result.Value.MatchItems.Should().BeEmpty();
        result.Value.TrueFalseKey.Should().BeNull();
        result.Value.Update(Content(type, [Criterion("Theme", 20)])).IsSuccess.Should().BeTrue("the rubric can be written on the draft later");
        result.Value.Rubric.Should().ContainSingle().Which.MaxPoints.Should().Be(20);
    }

    // AC2, BR3: stored trimmed, in the order written, positions from 1; an empty description is none.
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithThreeCriteria_StoresThemTrimmedInOrderFromPosition1(QuestionType type)
    {
        var result = Question.Create(Content(type, [Criterion("  Theme ", 30, " Meets the prompt "), Criterion("Argument", 40, "   "), Criterion("Language", 30)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Rubric.Select(criterion => criterion.Position).Should().Equal(1, 2, 3);
        result.Value.Rubric.Select(criterion => criterion.Name).Should().Equal("Theme", "Argument", "Language");
        result.Value.Rubric.Select(criterion => criterion.Description).Should().Equal("Meets the prompt", null, null);
        result.Value.Rubric.Select(criterion => criterion.MaxPoints).Should().Equal(30, 40, 30);
    }

    // AC3, BR3, BR5: the limits of the rubric, each with the position of its criterion; nothing is stored.
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithElevenCriteria_IsRefusedWithCriteriaCount_AndTenIsAccepted(QuestionType type)
    {
        var eleven = Enumerable.Range(1, 11).Select(number => Criterion($"Criterion {number}")).ToList();

        CodeOf(Question.Create(Content(type, eleven))).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaCount);
        Question.Create(Content(type, eleven.Take(10).ToList())).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithAnEmptyName_IsRefusedWithNameRequired_AtThePositionOfTheCriterion(string? name)
    {
        var rubric = new[] { Criterion("Theme"), Criterion(name) };

        CodeOf(Question.Create(Content(QuestionType.Essay, rubric))).Should().Be(QuestionBankErrorCodes.QuestionCriterionNameRequired);
        CheckOf(QuestionType.Essay, rubric)!.Position.Should().Be(2);
    }

    [Fact]
    public void Create_WithANameOf101Characters_IsRefusedWithNameTooLong_And100IsAccepted()
    {
        var tooLong = new string('a', QuestionLimits.CriterionNameMaxLength + 1);
        var longest = new string('a', QuestionLimits.CriterionNameMaxLength);

        CodeOf(Question.Create(Content(QuestionType.OpenAnswer, [Criterion(tooLong)]))).Should().Be(QuestionBankErrorCodes.QuestionCriterionNameTooLong);
        CheckOf(QuestionType.OpenAnswer, [Criterion(tooLong)])!.Position.Should().Be(1);
        Question.Create(Content(QuestionType.OpenAnswer, [Criterion($"  {longest}  ")])).IsSuccess.Should().BeTrue("the name is trimmed before it is measured");
    }

    [Fact]
    public void Create_WithADescriptionOf501Characters_IsRefusedWithDescriptionTooLong_And500IsAccepted()
    {
        var tooLong = new string('a', QuestionLimits.CriterionDescriptionMaxLength + 1);
        var longest = new string('a', QuestionLimits.CriterionDescriptionMaxLength);

        CodeOf(Question.Create(Content(QuestionType.Essay, [Criterion("Theme", 10, tooLong)])))
            .Should().Be(QuestionBankErrorCodes.QuestionCriterionDescriptionTooLong);
        Question.Create(Content(QuestionType.Essay, [Criterion("Theme", 10, longest)])).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Create_WithPointsMissingOrOutsideOneTo100_IsRefusedWithPointsOutOfRange(int? points)
    {
        CodeOf(Question.Create(Content(QuestionType.Essay, [Criterion("Theme", points)])))
            .Should().Be(QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange);
        CheckOf(QuestionType.Essay, [Criterion("Theme", points)])!.Position.Should().Be(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_WithPointsAtTheEdges_IsAccepted(int points)
    {
        var result = Question.Create(Content(QuestionType.OpenAnswer, [Criterion("Theme", points)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Rubric.Should().ContainSingle().Which.MaxPoints.Should().Be(points);
    }

    [Fact]
    public void Create_WithTwoNamesEqualUnderTheRule_IsRefusedWithNameRepeated_AtTheLaterCriterion()
    {
        var rubric = new[] { Criterion("Theme"), Criterion("Argument"), Criterion(" theme  ") };

        CodeOf(Question.Create(Content(QuestionType.Essay, rubric))).Should().Be(QuestionBankErrorCodes.QuestionCriterionNameRepeated);
        CheckOf(QuestionType.Essay, rubric)!.Position.Should().Be(3);
        Question.Create(Content(QuestionType.Essay, [Criterion("Theme"), Criterion("Themes")])).IsSuccess.Should().BeTrue("a different name is accepted");
    }

    [Fact]
    public void Update_WithABrokenRubric_IsRefused_AndNothingChanges()
    {
        var question = Published(QuestionType.Essay);

        var result = question.Update(Content(QuestionType.Essay, [Criterion("Theme", 0)], "<p>Outro tema</p>"));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange);
        question.StatementHtml.Should().Be("<p>Discuta o tema.</p>");
        question.Rubric.Select(criterion => criterion.Name).Should().Equal("Theme", "Argument");
    }

    // AC4, BR6
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Publish_WithNoCriterion_IsRefusedWithCriteriaRequired_AndWithOne_BecomesPublished(QuestionType type)
    {
        var question = Question.Create(Content(type)).Value;

        var refused = question.Publish();

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
        refused.Error!.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
        Question.Create(Content(type, [Criterion("Theme")])).Value.Publish().IsSuccess.Should().BeTrue();
    }

    // AC5, BR6
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Update_Published_RemovingEveryCriterion_IsRefused_AndNothingChanges(QuestionType type)
    {
        var question = Published(type);

        var result = question.Update(Content(type, [], "<p>Outro enunciado</p>"));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
        question.StatementHtml.Should().Be("<p>Discuta o tema.</p>");
        question.Rubric.Should().HaveCount(2);
    }

    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Update_Published_WithAnotherValidRubric_StaysPublished(QuestionType type)
    {
        var question = Published(type);

        question.Update(Content(type, [Criterion("Clarity", 50)])).IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
        question.Rubric.Should().ContainSingle().Which.Name.Should().Be("Clarity");
    }

    // AC6, BR7: an essay is limited in lines, an open answer in characters.
    [Fact]
    public void Create_AnEssayWithSevenToThirtyLines_StoresBoth()
    {
        var question = Question.Create(Content(QuestionType.Essay, sizeMin: 7, sizeMax: 30)).Value;

        question.AnswerSizeMin.Should().Be(7);
        question.AnswerSizeMax.Should().Be(30);
    }

    [Theory]
    [InlineData(31, 30)]
    [InlineData(0, 30)]
    [InlineData(101, null)]
    [InlineData(null, 0)]
    [InlineData(null, 101)]
    [InlineData(5, 101)]
    public void Create_AnEssayWithAnInvalidLimit_IsRefusedWithAnswerSizeInvalid(int? min, int? max)
    {
        CodeOf(Question.Create(Content(QuestionType.Essay, sizeMin: min, sizeMax: max))).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, 30)]
    [InlineData(1, null)]
    [InlineData(100, 100)]
    public void Create_AnEssayWithOnlyAMaximumOrNone_IsAccepted(int? min, int? max)
    {
        var result = Question.Create(Content(QuestionType.Essay, sizeMin: min, sizeMax: max));

        result.IsSuccess.Should().BeTrue();
        (result.Value.AnswerSizeMin, result.Value.AnswerSizeMax).Should().Be((min, max));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(5000)]
    public void Create_AnOpenAnswerWithAMaximumOfCharacters_StoresIt(int max)
    {
        var question = Question.Create(Content(QuestionType.OpenAnswer, sizeMax: max)).Value;

        question.AnswerSizeMax.Should().Be(max);
        question.AnswerSizeMin.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 5001)]
    [InlineData(10, null)]
    [InlineData(10, 500)]
    public void Create_AnOpenAnswerWithAMinimumOrAMaximumOutOfRange_IsRefusedWithAnswerSizeInvalid(int? min, int? max)
    {
        CodeOf(Question.Create(Content(QuestionType.OpenAnswer, sizeMin: min, sizeMax: max))).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
    }

    // AC7, BR8: nothing is ignored.
    [Theory]
    [MemberData(nameof(OtherTypes))]
    public void Create_OfAnotherType_WithARubric_IsRefusedWithRubricNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, [Criterion("Theme")]))).Should().Be(QuestionBankErrorCodes.QuestionRubricNotAllowed);
        Question.Create(Content(type)).IsSuccess.Should().BeTrue("the same content without the rubric is accepted");
    }

    [Theory]
    [MemberData(nameof(OtherTypes))]
    public void Create_OfAnotherType_WithASizeLimit_IsRefusedWithAnswerSizeNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, sizeMax: 30))).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeNotAllowed);
        CodeOf(Question.Create(Content(type, sizeMin: 5))).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeNotAllowed);
    }

    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithOptions_IsRefusedWithOptionsNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, [Criterion("Theme")], options: [new OptionDraft(Text("A"), false)])))
            .Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
    }

    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithATrueFalseKey_IsRefusedWithKeyNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, [Criterion("Theme")], trueFalseKey: true))).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        Question.Create(Content(type, [Criterion("Theme")])).Value.Rubric.Should().ContainSingle("the same content without the key is accepted");
    }

    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithMatchItems_IsRefusedWithMatchItemsNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, [Criterion("Theme")], matchItems: [new MatchItemDraft(Text("x"), null)])))
            .Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        Question.Create(Content(type, [Criterion("Theme")])).Value.Rubric.Should().ContainSingle("the same content without the items is accepted");
    }

    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithBlanks_IsRefusedWithBlanksNotAllowed(QuestionType type)
    {
        CodeOf(Question.Create(Content(type, [Criterion("Theme")], blanks: [new BlankDraft(1, ["x"])])))
            .Should().Be(QuestionBankErrorCodes.QuestionBlanksNotAllowed);
        Question.Create(Content(type, [Criterion("Theme")])).Value.Rubric.Should().ContainSingle("the same content without the blanks is accepted");
    }

    // AC8, BR2
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Update_WithContentOfAnotherType_IsAConflict(QuestionType type)
    {
        var question = Published(type);

        var result = question.Update(Content(QuestionType.SingleChoice, statement: "<p>Mudou</p>"));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        result.Error!.Kind.Should().Be(ErrorKind.Conflict);
        question.Type.Should().Be(type);
        question.Rubric.Should().HaveCount(2);
        question.Update(Content(type, [Criterion("Clarity")])).IsSuccess.Should().BeTrue("naming its own type is accepted");
    }

    // AC9, BR8: [[1]] is plain statement text, no marker is read.
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsText(QuestionType type)
    {
        var result = Question.Create(Content(type, [Criterion("Theme")], "<p>Escreva [[1]] como está.</p>"));

        result.IsSuccess.Should().BeTrue();
        result.Value.StatementHtml.Should().Contain("[[1]]");
        result.Value.Blanks.Should().BeEmpty();
        result.Value.Rubric.Should().ContainSingle();
    }

    // AC10, BR10: a criterion added, removed, renamed or with other points is a key change that needs its reason.
    [Theory]
    [InlineData(QuestionType.OpenAnswer, "points")]
    [InlineData(QuestionType.Essay, "points")]
    [InlineData(QuestionType.Essay, "added")]
    [InlineData(QuestionType.Essay, "removed")]
    [InlineData(QuestionType.OpenAnswer, "renamed")]
    public void NextVersion_ChangingTheKeyOfTheRubric_NeedsAReason_AndThenRecordsAKeyChangeWithTheRubric(QuestionType type, string change)
    {
        var question = Published(type);
        var first = question.NextVersion(null, null).Value!;
        first.AnswerKeyChanged.Should().BeFalse("version 1 is never a key change");
        first.Rubric.Select(criterion => (criterion.Position, criterion.Name, criterion.MaxPoints)).Should().Equal((1, "Theme", 30), (2, "Argument", 40));
        IReadOnlyList<CriterionDraft> changed = change switch
        {
            "points" => [Criterion("Theme", 30), Criterion("Argument", 45)],
            "added" => [Criterion("Theme", 30), Criterion("Argument", 40), Criterion("Language", 10)],
            "removed" => [Criterion("Theme", 30)],
            _ => [Criterion("Theme", 30), Criterion("Reasoning", 40)]
        };
        question.Update(Content(type, changed)).IsSuccess.Should().BeTrue();

        var refused = question.NextVersion(first, null);
        var recorded = question.NextVersion(first, "A banca mudou o critério.");

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        recorded.Value!.Number.Should().Be(2);
        recorded.Value.AnswerKeyChanged.Should().BeTrue();
        recorded.Value.Reason.Should().Be("A banca mudou o critério.");
        recorded.Value.Rubric.Select(criterion => (criterion.Name, criterion.MaxPoints)).Should().Equal(changed.Select(criterion => (criterion.Name!, criterion.MaxPoints!.Value)));
        first.Rubric.Should().HaveCount(2, "a version is never edited");
    }

    // AC11, BR10: only a description, the order, the statement, the explanation or the size is not a key change.
    [Fact]
    public void NextVersion_ChangingOnlyADescription_IsAVersionWithNoKeyChange()
    {
        var question = Published(QuestionType.Essay);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Content(QuestionType.Essay, [Criterion("Theme", 30, "Now described"), Criterion("Argument", 40)])).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value.Should().NotBeNull("the description is versioned content");
        result.Value!.AnswerKeyChanged.Should().BeFalse();
        result.Value.Rubric[0].Description.Should().Be("Now described");
    }

    [Fact]
    public void NextVersion_ReorderingTheCriteria_IsAVersionWithNoKeyChange()
    {
        var question = Published(QuestionType.OpenAnswer);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Content(QuestionType.OpenAnswer, [Criterion("Argument", 40), Criterion("Theme", 30)])).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value.Should().NotBeNull("the order is versioned content");
        result.Value!.AnswerKeyChanged.Should().BeFalse();
        result.Value.Rubric.Select(criterion => criterion.Name).Should().Equal("Argument", "Theme");
    }

    [Fact]
    public void NextVersion_ChangingTheNameOnlyInCaseOrSpacing_IsNotAKeyChange()
    {
        var question = Published(QuestionType.Essay);
        var first = question.NextVersion(null, null).Value!;
        question.Update(Content(QuestionType.Essay, [Criterion(" THEME ", 30), Criterion("Argument", 40)])).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value!.AnswerKeyChanged.Should().BeFalse();
    }

    [Theory]
    [InlineData("statement")]
    [InlineData("explanation")]
    [InlineData("size")]
    public void NextVersion_ChangingOnlyTheStatementTheExplanationOrTheSize_IsAVersionWithNoKeyChange(string change)
    {
        var question = Published(QuestionType.Essay);
        var first = question.NextVersion(null, null).Value!;
        var rubric = new[] { Criterion("Theme", 30), Criterion("Argument", 40) };
        var content = change switch
        {
            "statement" => Content(QuestionType.Essay, rubric, "<p>Outro tema.</p>"),
            "explanation" => Content(QuestionType.Essay, rubric, explanation: "<p>Modelo de resposta.</p>"),
            _ => Content(QuestionType.Essay, rubric, sizeMin: 7, sizeMax: 30)
        };
        question.Update(content).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull("the change is versioned content");
        result.Value!.AnswerKeyChanged.Should().BeFalse();
    }

    [Fact]
    public void NextVersion_RecordsTheSizeLimitTheQuestionHad()
    {
        var question = Question.Create(Content(QuestionType.Essay, [Criterion("Theme")], sizeMin: 7, sizeMax: 30)).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        var first = question.NextVersion(null, null).Value!;

        first.AnswerSizeMin.Should().Be(7);
        first.AnswerSizeMax.Should().Be(30);
    }

    [Fact]
    public void NextVersion_NothingChanged_MakesNothing()
    {
        var question = Published(QuestionType.Essay);
        var first = question.NextVersion(null, null).Value!;

        question.NextVersion(first, null).Value.Should().BeNull();
    }

    // AC12, BR11: annulment and reinstatement keep the rubric and the size of the version they copy.
    [Theory]
    [MemberData(nameof(RubricTypes))]
    public void AnnulAndReinstate_CopyTheRubricAndTheSizeOfTheLatestVersion(QuestionType type)
    {
        var question = Question.Create(Content(type, [Criterion("Theme", 30, "Meets"), Criterion("Argument", 40)], sizeMax: type == QuestionType.Essay ? 30 : 500)).Value;
        question.Publish().IsSuccess.Should().BeTrue();
        var first = question.NextVersion(null, null).Value!;

        var annulled = question.Annul(first, "Anulada pela banca.").Value!;
        var reinstated = question.Reinstate(annulled, "Recurso indeferido.").Value!;

        annulled.Rubric.Select(criterion => (criterion.Position, criterion.Name, criterion.Description, criterion.MaxPoints))
            .Should().Equal((1, "Theme", "Meets", 30), (2, "Argument", null, 40));
        reinstated.Rubric.Should().HaveCount(2);
        reinstated.AnswerSizeMax.Should().Be(type == QuestionType.Essay ? 30 : 500);
        question.NextVersion(reinstated, null).Value.Should().BeNull("the copy is the same versioned content");
    }
}
