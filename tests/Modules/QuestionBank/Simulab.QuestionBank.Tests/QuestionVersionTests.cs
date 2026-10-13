using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-98, the version rules without a database: what is versioned (BR1), when a save or a publish makes a
/// version (BR2, BR3), what counts as an answer key change (BR4) and the reason it needs (BR5).
/// </summary>
public sealed class QuestionVersionTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static QuestionContent SingleChoice(
        string statement = "<p>Qual é a capital da França?</p>",
        bool parisIsCorrect = true,
        string? explanation = null,
        int? difficulty = null,
        Guid? topic = null,
        Guid? edition = null,
        int? paperNumber = null,
        bool swap = false)
    {
        var paris = new OptionDraft(Text("Paris"), parisIsCorrect);
        var lyon = new OptionDraft(Text("Lyon"), !parisIsCorrect);
        IReadOnlyList<OptionDraft> options = swap ? [lyon, paris] : [paris, lyon];

        return new QuestionContent(
            topic ?? Topic,
            "pt-BR",
            Text(statement),
            SanitizedHtml.Empty,
            explanation is null ? SanitizedHtml.Empty : Text(explanation),
            difficulty,
            options,
            edition,
            paperNumber);
    }

    private static QuestionContent TrueFalse(bool? key, string statement = "<p>Paris é a capital da França.</p>") =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            SanitizedHtml.Empty,
            SanitizedHtml.Empty,
            null,
            [],
            Type: QuestionType.TrueFalse,
            TrueFalseKey: key);

    private static Question Published(QuestionContent content)
    {
        var question = Question.Create(content).Value;
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    // The version a first publication makes: what the tests below take as "the latest version".
    private static QuestionVersion FirstVersion(Question question)
    {
        var result = question.NextVersion(null, null);
        result.IsSuccess.Should().BeTrue();

        return result.Value!;
    }

    // AC1, BR2: the first publication makes version 1 with the stored content, no key change, no reason.
    [Fact]
    public void NextVersion_FirstPublication_MakesVersionOneWithTheStoredContent()
    {
        var question = Published(SingleChoice(explanation: "<p>É Paris.</p>"));

        var result = question.NextVersion(null, null);

        result.IsSuccess.Should().BeTrue();
        var version = result.Value!;
        version.Number.Should().Be(1);
        version.QuestionId.Should().Be(question.Id);
        version.Type.Should().Be(QuestionType.SingleChoice);
        version.ContentLanguage.Should().Be("pt-BR");
        version.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        version.BaseTextHtml.Should().BeNull();
        version.ExplanationHtml.Should().Be("<p>É Paris.</p>");
        version.TrueFalseKey.Should().BeNull();
        version.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect))
            .Should().Equal((0, "Paris", true), (1, "Lyon", false));
        version.AnswerKeyChanged.Should().BeFalse();
        version.Reason.Should().BeNull();
    }

    // BR2, BR3: a draft never makes a version, whatever it holds.
    [Fact]
    public void NextVersion_OnADraft_MakesNothing()
    {
        var question = Question.Create(SingleChoice()).Value;

        var result = question.NextVersion(null, "uma nota");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // AC2, BR3, BR6: a new statement makes version N+1; the earlier one is a separate object that keeps the old text.
    [Fact]
    public void NextVersion_ChangedStatement_MakesTheNextNumber_AndTheLatestKeepsItsText()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(statement: "<p>Qual é a capital da Espanha?</p>")).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(2);
        result.Value.StatementHtml.Should().Be("<p>Qual é a capital da Espanha?</p>");
        result.Value.AnswerKeyChanged.Should().BeFalse();
        first.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
    }

    // AC3, BR3: the same content makes none.
    [Fact]
    public void NextVersion_SameContent_MakesNothing()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice()).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // AC3, BR1: topic, difficulty and origin are catalog data; changing only them makes none.
    [Fact]
    public void NextVersion_OnlyTopicDifficultyAndOriginChanged_MakesNothing()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(
            difficulty: 4, topic: Guid.CreateVersion7(), edition: Guid.CreateVersion7(), paperNumber: 12))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // BR1: the explanation is part of what is versioned.
    [Fact]
    public void NextVersion_ChangedExplanation_MakesTheNextNumber()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(explanation: "<p>Porque Paris é a capital.</p>")).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.Value!.Number.Should().Be(2);
        result.Value.ExplanationHtml.Should().Be("<p>Porque Paris é a capital.</p>");
    }

    // AC4, BR4, BR5: the key moves to another option: a reason is required, and nothing is made without it.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NextVersion_KeyMoved_WithNoReason_IsRefusedWithKeyChangeReasonRequired(string? reason)
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(parisIsCorrect: false)).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, reason);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void NextVersion_KeyMoved_WithAReason_MakesAVersionMarkedAsKeyChangedWithTheTrimmedReason()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(parisIsCorrect: false)).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, "  Recurso deferido pela banca.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(2);
        result.Value.AnswerKeyChanged.Should().BeTrue();
        result.Value.Reason.Should().Be("Recurso deferido pela banca.");
    }

    // AC5, BR5: a reason of exactly 500 characters is fine, one more is refused.
    [Fact]
    public void NextVersion_ReasonOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithChangeReasonTooLong()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(parisIsCorrect: false)).IsSuccess.Should().BeTrue();

        var accepted = question.NextVersion(first, new string('a', QuestionLimits.ChangeReasonMaxLength));
        var refused = question.NextVersion(first, new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        accepted.IsSuccess.Should().BeTrue();
        refused.IsFailure.Should().BeTrue();
        refused.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionChangeReasonTooLong);
        refused.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    // AC5, BR5: the limit counts the trimmed text.
    [Fact]
    public void NextVersion_ReasonWhoseTrimmedLengthIsTheLimit_IsAccepted()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(parisIsCorrect: false)).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, "   " + new string('a', QuestionLimits.ChangeReasonMaxLength) + "   ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Reason.Should().HaveLength(QuestionLimits.ChangeReasonMaxLength);
    }

    // AC6, BR4: a reorder that moves the correct option to another letter is a key change.
    [Fact]
    public void NextVersion_ReorderThatMovesTheCorrectOption_IsAKeyChangeAndNeedsTheReason()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(swap: true)).IsSuccess.Should().BeTrue();

        var withoutReason = question.NextVersion(first, null);
        var withReason = question.NextVersion(first, "A banca reordenou as alternativas.");

        withoutReason.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        withReason.Value!.AnswerKeyChanged.Should().BeTrue();
        withReason.Value.Options.Single(option => option.IsCorrect).Position.Should().Be(1);
    }

    // BR4: a reorder that leaves the correct option on its letter changes the options but not the key.
    [Fact]
    public void NextVersion_ReorderThatKeepsTheCorrectLetter_IsNotAKeyChange()
    {
        var question = Published(new QuestionContent(
            Topic,
            "pt-BR",
            Text("<p>Qual?</p>"),
            SanitizedHtml.Empty,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("A"), true), new OptionDraft(Text("B"), false), new OptionDraft(Text("C"), false)]));
        var first = FirstVersion(question);
        question.Update(new QuestionContent(
            Topic,
            "pt-BR",
            Text("<p>Qual?</p>"),
            SanitizedHtml.Empty,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("A"), true), new OptionDraft(Text("C"), false), new OptionDraft(Text("B"), false)]))
            .IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(2);
        result.Value.AnswerKeyChanged.Should().BeFalse();
    }

    // AC7, BR4: a true/false key that flips is a key change.
    [Fact]
    public void NextVersion_TrueFalseKeyFlipped_IsAKeyChangeAndNeedsTheReason()
    {
        var question = Published(TrueFalse(true));
        var first = FirstVersion(question);
        first.Type.Should().Be(QuestionType.TrueFalse);
        first.TrueFalseKey.Should().BeTrue();
        first.Options.Should().BeEmpty();
        question.Update(TrueFalse(false)).IsSuccess.Should().BeTrue();

        var withoutReason = question.NextVersion(first, null);
        var withReason = question.NextVersion(first, "Gabarito oficial retificado.");

        withoutReason.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        withReason.Value!.AnswerKeyChanged.Should().BeTrue();
        withReason.Value.TrueFalseKey.Should().BeFalse();
    }

    // AC8, BR5: a reason on a version whose key did not change is kept as its note.
    [Fact]
    public void NextVersion_ChangedStatementWithAReason_KeepsTheReasonAsANote_AndTheKeyIsNotMarkedChanged()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(statement: "<p>Nova redação.</p>")).IsSuccess.Should().BeTrue();

        var result = question.NextVersion(first, "Correção de redação.");

        result.Value!.AnswerKeyChanged.Should().BeFalse();
        result.Value.Reason.Should().Be("Correção de redação.");
    }

    // AC8, BR5: a reason where no version is made is ignored, even a blank or an over-long one.
    [Theory]
    [InlineData("uma nota")]
    [InlineData("   ")]
    public void NextVersion_SameContentWithAReason_MakesNothing(string reason)
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);

        var result = question.NextVersion(first, reason);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void NextVersion_SameContentWithAReasonOverTheLimit_IsIgnored()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);

        var result = question.NextVersion(first, new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // BR4: version 1 is never marked as a key change, whatever reason came with it; the reason stays as a note.
    [Fact]
    public void NextVersion_VersionOne_IsNeverAKeyChange_AndKeepsAReasonAsANote()
    {
        var question = Published(SingleChoice());

        var result = question.NextVersion(null, "Primeira publicação.");

        result.Value!.Number.Should().Be(1);
        result.Value.AnswerKeyChanged.Should().BeFalse();
        result.Value.Reason.Should().Be("Primeira publicação.");
    }

    // BR3: numbers follow the latest version handed in, not a count.
    [Fact]
    public void NextVersion_AfterVersionTwo_MakesVersionThree()
    {
        var question = Published(SingleChoice());
        var first = FirstVersion(question);
        question.Update(SingleChoice(statement: "<p>Segunda.</p>")).IsSuccess.Should().BeTrue();
        var second = question.NextVersion(first, null).Value!;
        question.Update(SingleChoice(statement: "<p>Terceira.</p>")).IsSuccess.Should().BeTrue();

        var third = question.NextVersion(second, null);

        third.Value!.Number.Should().Be(3);
    }
}
