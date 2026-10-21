using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-106, the rules of a matching question without a database: what a draft and a published question need (BR3,
/// BR4), the shapes that are refused (BR5, BR6), the images counted with the left items (BR7), the key change check
/// over the matches (BR8) and the copy an annulment makes.
/// </summary>
public sealed class MatchingQuestionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static OptionDraft Right(string text) => new(Text(text), false);

    private static MatchItemDraft Left(string text, int? match) => new(Text(text), match);

    private static IReadOnlyList<OptionDraft> Rights(int count) =>
        [.. Enumerable.Range(0, count).Select(index => Right($"Direita {index}"))];

    private static IReadOnlyList<MatchItemDraft> Lefts(params int?[] matches) =>
        [.. matches.Select((match, index) => Left($"Esquerda {index}", match))];

    private static QuestionContent Matching(
        IReadOnlyList<OptionDraft>? rights = null,
        IReadOnlyList<MatchItemDraft>? lefts = null,
        string statement = "<p>Associe as colunas.</p>",
        bool? trueFalseKey = null,
        QuestionType type = QuestionType.Matching) =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            null,
            SanitizedHtml.Empty,
            null,
            rights ?? [],
            Type: type,
            TrueFalseKey: trueFalseKey,
            MatchItems: lefts);

    // 1→C, 2→A, 3→B over three right items.
    private static Question Published()
    {
        var question = Question.Create(Matching(Rights(3), Lefts(2, 0, 1))).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    private static string? CodeOf(Result result) => result.IsFailure ? result.Error!.Code : null;

    // AC1, BR1, BR3: a draft with no items is stored with its type.
    [Fact]
    public void Create_MatchingWithNoItems_IsADraftOfThatType()
    {
        var result = Question.Create(Matching());

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(QuestionType.Matching);
        result.Value.Status.Should().Be(QuestionStatus.Draft);
        result.Value.Options.Should().BeEmpty();
        result.Value.MatchItems.Should().BeEmpty();
        result.Value.TrueFalseKey.Should().BeNull();
    }

    // AC2, BR3: up to eight on each side, matches set or not, are a valid draft and are kept as sent.
    [Fact]
    public void Create_MatchingDraftWithEightItemsEachSide_AndSomeMatchesUnset_KeepsThemAsSent()
    {
        var result = Question.Create(Matching(Rights(8), Lefts(7, null, 0, 1, null, 2, 3, 4)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Options.Should().HaveCount(8).And.OnlyContain(option => !option.IsCorrect);
        result.Value.MatchItems.OrderBy(item => item.Position).Select(item => item.MatchPosition)
            .Should().Equal(7, null, 0, 1, null, 2, 3, 4);
        result.Value.MatchItems.OrderBy(item => item.Position).Select(item => item.TextHtml)
            .Should().StartWith("Esquerda 0");
    }

    [Fact]
    public void Create_MatchingWithANinthLeftItem_IsRefusedWithMatchingItemsCount()
    {
        var result = Question.Create(Matching(Rights(2), Lefts(null, null, null, null, null, null, null, null, null)));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
    }

    [Fact]
    public void Create_MatchingWithANinthRightItem_IsRefusedWithMatchingItemsCount()
    {
        var result = Question.Create(Matching(Rights(9), Lefts(0)));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
    }

    // AC3, BR3: broken data is refused even on a draft.
    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    public void Create_MatchingDraftNamingARightItemThatDoesNotExist_IsRefusedWithMatchingKeyInvalid(int match)
    {
        var result = Question.Create(Matching(Rights(3), Lefts(0, match)));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
    }

    [Fact]
    public void Create_MatchingDraftWithTwoItemsOnTheSameRightItem_IsRefusedWithMatchingKeyRepeated()
    {
        var result = Question.Create(Matching(Rights(3), Lefts(1, 1)));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyRepeated);
    }

    // AC4, BR4: two to eight left items, at least as many right items, every text written.
    [Fact]
    public void Publish_MatchingWithOneLeftItem_IsRefusedWithMatchingItemsCount()
    {
        var question = Question.Create(Matching(Rights(2), Lefts(0))).Value;

        var result = question.Publish();

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Publish_MatchingWithFewerRightItemsThanLeftItems_IsRefusedWithMatchingItemsCount()
    {
        var question = Question.Create(Matching(Rights(2), Lefts(0, 1, null))).Value;

        CodeOf(question.Publish()).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
    }

    [Fact]
    public void Publish_MatchingWithAnEmptyRightText_IsRefusedWithOptionTextRequired()
    {
        var question = Question.Create(Matching(
            [Right("Paris"), new OptionDraft(SanitizedHtml.Empty, false)], Lefts(0, 1))).Value;

        CodeOf(question.Publish()).Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
    }

    [Fact]
    public void Publish_MatchingWithAnEmptyLeftText_IsRefusedWithMatchItemTextRequired()
    {
        var question = Question.Create(Matching(
            Rights(2), [Left("França", 0), new MatchItemDraft(SanitizedHtml.Empty, 1)])).Value;

        CodeOf(question.Publish()).Should().Be(QuestionBankErrorCodes.QuestionMatchItemTextRequired);
    }

    // AC5, BR4: every left item matched; extra right items are fine.
    [Fact]
    public void Publish_MatchingWithAnItemWithNoMatch_IsRefusedWithMatchingKeyInvalid()
    {
        var question = Question.Create(Matching(Rights(3), Lefts(0, null))).Value;

        CodeOf(question.Publish()).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
    }

    [Fact]
    public void Publish_MatchingWithEveryItemMatched_AndExtraRightItems_BecomesPublished()
    {
        var question = Question.Create(Matching(Rights(5), Lefts(4, 0, 2))).Value;

        question.Publish().IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
    }

    // AC6, BR4: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public void Update_PublishedMatching_LeavingAnItemWithNoMatch_IsRefused_AndNothingChanges()
    {
        var question = Published();

        var result = question.Update(Matching(Rights(3), Lefts(2, 0, null), "<p>Outro.</p>"));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
        question.StatementHtml.Should().Be("<p>Associe as colunas.</p>");
        question.MatchItems.OrderBy(item => item.Position).Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
    }

    [Fact]
    public void Update_PublishedMatching_ReducingToOneItem_IsRefusedWithMatchingItemsCount()
    {
        var question = Published();

        CodeOf(question.Update(Matching(Rights(3), Lefts(2)))).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        question.MatchItems.Should().HaveCount(3);
    }

    [Fact]
    public void Update_PublishedMatching_WithAnotherValidSet_StaysPublished()
    {
        var question = Published();

        question.Update(Matching(Rights(3), Lefts(0, 2, 1))).IsSuccess.Should().BeTrue();

        question.Status.Should().Be(QuestionStatus.Published);
        question.MatchItems.OrderBy(item => item.Position).Select(item => item.MatchPosition).Should().Equal(0, 2, 1);
    }

    // AC7, BR5: a true/false key or a marked right item is refused, never ignored.
    [Fact]
    public void Create_MatchingWithATrueFalseKey_IsRefusedWithKeyNotAllowed()
    {
        CodeOf(Question.Create(Matching(Rights(2), Lefts(0, 1), trueFalseKey: true)))
            .Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
    }

    [Fact]
    public void Create_MatchingWithAMarkedRightItem_IsRefusedWithKeyNotAllowed()
    {
        CodeOf(Question.Create(Matching([Right("A"), new OptionDraft(Text("B"), true)], Lefts(0, 1))))
            .Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
    }

    // AC7, BR6: left items on any other type are refused.
    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    [InlineData(QuestionType.MultipleAnswer)]
    public void Create_AnotherTypeWithMatchItems_IsRefusedWithMatchItemsNotAllowed(QuestionType type)
    {
        var result = Question.Create(Matching([], Lefts(null), type: type));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
    }

    // AC8, BR2: the type never changes.
    [Fact]
    public void Update_MatchingWithContentOfAnotherType_IsAConflict()
    {
        var question = Question.Create(Matching(Rights(2), Lefts(0, 1))).Value;

        var result = question.Update(Matching(Rights(2), type: QuestionType.SingleChoice));

        CodeOf(result).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        result.Error!.Kind.Should().Be(ErrorKind.Conflict);
        question.Type.Should().Be(QuestionType.Matching);
    }

    // AC11, BR7: the left items count with the statement and the options under the ten-image cap.
    [Fact]
    public void Create_MatchingWithElevenImagesAcrossStatementOptionsAndItems_IsRefusedWithImagesTooMany()
    {
        static SanitizedHtml Pictured(int count) =>
            new("<p>x</p>", "x") { ImageIds = [.. Enumerable.Range(0, count).Select(_ => Guid.CreateVersion7())] };

        var content = Matching(
            [new OptionDraft(Pictured(4), false), Right("B")],
            [new MatchItemDraft(Pictured(4), 0), Left("Dois", 1)]) with { Statement = Pictured(3) };

        CodeOf(Question.Create(content)).Should().Be(QuestionBankErrorCodes.QuestionImagesTooMany);
    }

    [Fact]
    public void Create_MatchingWithALeftItemImageWithoutAlt_IsRefusedWithImageAltRequired()
    {
        var pictured = new SanitizedHtml("<p>x</p>", "x") { ImageIds = [Guid.CreateVersion7()], ImagesWithoutAlt = 1 };

        CodeOf(Question.Create(Matching(Rights(2), [new MatchItemDraft(pictured, 0)])))
            .Should().Be(QuestionBankErrorCodes.QuestionImageAltRequired);
    }

    [Fact]
    public void Create_MatchingWithALeftItemOverTheOptionLimit_IsRefusedWithMatchItemTooLong()
    {
        var longText = new string('a', QuestionLimits.OptionMaxLength + 1);

        CodeOf(Question.Create(Matching(Rights(2), [Left(longText, 0)])))
            .Should().Be(QuestionBankErrorCodes.QuestionMatchItemTooLong);
    }

    // AC9, BR8: a change of what a number matches is a key change and needs its reason.
    [Fact]
    public void NextVersion_ChangingWhatANumberMatches_NeedsAReason_AndThenRecordsAKeyChangeWithTheItems()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        first.MatchItems.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
        question.Update(Matching(Rights(3), Lefts(0, 2, 1))).IsSuccess.Should().BeTrue();

        var refused = question.NextVersion(first, null);
        var recorded = question.NextVersion(first, "Gabarito retificado.");

        CodeOf(refused).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        recorded.Value!.AnswerKeyChanged.Should().BeTrue();
        recorded.Value.Reason.Should().Be("Gabarito retificado.");
        recorded.Value.MatchItems.Select(item => item.MatchPosition).Should().Equal(0, 2, 1);
    }

    // AC10, BR8: a text edit is a version, not a key change; adding an item is a key change.
    [Fact]
    public void NextVersion_ChangingOnlyTheTextOfAnItem_IsAVersionWithNoKeyChange()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(Matching(
            [Right("Outro texto"), Right("Direita 1"), Right("Direita 2")],
            [Left("Outro item", 2), Left("Esquerda 1", 0), Left("Esquerda 2", 1)])).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AnswerKeyChanged.Should().BeFalse();
        result.Value.MatchItems.First().TextHtml.Should().Be("Outro item");
    }

    [Fact]
    public void NextVersion_AddingALeftItem_NeedsAReason()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;
        question.Update(Matching(Rights(4), Lefts(2, 0, 1, 3))).IsSuccess.Should().BeTrue();

        CodeOf(question.NextVersion(first, null)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    [Fact]
    public void NextVersion_NothingChanged_MakesNothing()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;

        question.NextVersion(first, null).Value.Should().BeNull();
    }

    // AC11, BR9 (trap T3): annulment and reinstatement keep the left items of the version they copy.
    [Fact]
    public void AnnulAndReinstate_CopyTheMatchItemsOfTheLatestVersion()
    {
        var question = Published();
        var first = question.NextVersion(null, null).Value!;

        var annulled = question.Annul(first, "Anulada pela banca.").Value!;
        var reinstated = question.Reinstate(annulled, "Recurso indeferido.").Value!;

        annulled.MatchItems.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
        reinstated.MatchItems.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
        question.NextVersion(reinstated, null).Value.Should().BeNull("the copy is the same versioned content");
    }

    private static string? CodeOf<T>(Result<T> result) => result.IsFailure ? result.Error!.Code : null;
}
