using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-101, the annulment rules without a database: who can be annulled (BR1), the version an annulment and a
/// reinstatement make (BR2, BR6), the reason (BR3), what changes and what does not (BR4) and the frozen state
/// (BR5).
/// </summary>
public sealed class QuestionAnnulmentTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static QuestionContent SingleChoice(string statement = "<p>Qual é a capital da França?</p>") =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            Text("<p>Texto-base.</p>"),
            Text("<p>É Paris.</p>"),
            3,
            [new OptionDraft(Text("Paris"), true), new OptionDraft(Text("Lyon"), false)]);

    private static QuestionContent TrueFalse(bool key) =>
        new(
            Topic,
            "pt-BR",
            Text("<p>Paris é a capital da França.</p>"),
            SanitizedHtml.Empty,
            SanitizedHtml.Empty,
            null,
            [],
            Type: QuestionType.TrueFalse,
            TrueFalseKey: key);

    private static (Question Question, QuestionVersion Latest) Published(QuestionContent? content = null)
    {
        var question = Question.Create(content ?? SingleChoice()).Value;
        question.Publish().IsSuccess.Should().BeTrue();
        var first = question.NextVersion(null, null);
        first.IsSuccess.Should().BeTrue();

        return (question, first.Value!);
    }

    // AC1, BR2, BR4: the annulment is the next version, a copy of the latest with the flag and the trimmed reason.
    [Fact]
    public void Annul_APublishedQuestion_MakesTheNextVersionAnnulled_WithTheSameContentAndTheTrimmedReason()
    {
        var (question, latest) = Published();

        var result = question.Annul(latest, "  A banca anulou o item 12.  ");

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Annulled);
        var version = result.Value!;
        version.Number.Should().Be(2);
        version.QuestionId.Should().Be(question.Id);
        version.Annulled.Should().BeTrue();
        version.AnswerKeyChanged.Should().BeFalse();
        version.Reason.Should().Be("A banca anulou o item 12.");
        version.Type.Should().Be(QuestionType.SingleChoice);
        version.ContentLanguage.Should().Be("pt-BR");
        version.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        version.BaseTextHtml.Should().Be("<p>Texto-base.</p>");
        version.ExplanationHtml.Should().Be("<p>É Paris.</p>");
        version.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect))
            .Should().Equal((0, "Paris", true), (1, "Lyon", false));
        latest.Annulled.Should().BeFalse("an earlier version is never touched (BR2)");
        latest.Number.Should().Be(1);
    }

    // BR4: a true/false key travels with the copy and the question's own content does not change.
    [Fact]
    public void Annul_ATrueFalseQuestion_KeepsItsKey_AndChangesNothingButTheStatus()
    {
        var (question, latest) = Published(TrueFalse(true));

        var result = question.Annul(latest, "Anulada.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.TrueFalseKey.Should().BeTrue();
        result.Value.Options.Should().BeEmpty();
        question.TrueFalseKey.Should().BeTrue();
        question.StatementHtml.Should().Be("<p>Paris é a capital da França.</p>");
        question.Options.Should().BeEmpty();
    }

    // AC2, BR1: a draft is not annulled, and an annulled one is not annulled twice; nothing changes.
    [Fact]
    public void Annul_ADraft_IsRefusedWithNotPublished_AndNothingChanges()
    {
        var draft = Question.Create(SingleChoice()).Value;

        var result = draft.Annul(null, "Anulada.");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionNotPublished);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        draft.Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Annul_AnAnnulledQuestion_IsRefusedWithAlreadyAnnulled()
    {
        var (question, latest) = Published();
        var annulled = question.Annul(latest, "Anulada.").Value!;

        var again = question.Annul(annulled, "Outra vez.");

        again.IsFailure.Should().BeTrue();
        again.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionAlreadyAnnulled);
        again.Error.Kind.Should().Be(ErrorKind.Conflict);
        question.Status.Should().Be(QuestionStatus.Annulled);
    }

    // BR3: the status is judged before the reason, as a publish judges its state first.
    [Fact]
    public void Annul_ADraftWithABlankReason_AnswersTheStatusFirst()
    {
        var draft = Question.Create(SingleChoice()).Value;

        var result = draft.Annul(null, "   ");

        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionNotPublished);
    }

    // AC3, BR3: a missing, empty or blank reason is refused and the question stays published.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Annul_WithAMissingOrBlankReason_IsRefusedWithStatusReasonRequired_AndStaysPublished(string? reason)
    {
        var (question, latest) = Published();

        var result = question.Annul(latest, reason);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatusReasonRequired);
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public void Annul_AReasonOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithStatusReasonTooLong()
    {
        var (question, latest) = Published();

        var tooLong = question.Annul(latest, new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        tooLong.IsFailure.Should().BeTrue();
        tooLong.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatusReasonTooLong);
        tooLong.Error.Kind.Should().Be(ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Published);

        var accepted = question.Annul(latest, " " + new string('a', QuestionLimits.ChangeReasonMaxLength) + " ");

        accepted.IsSuccess.Should().BeTrue();
        accepted.Value!.Reason!.Length.Should().Be(QuestionLimits.ChangeReasonMaxLength);
    }

    // AC4, BR5: an annulled question is frozen; every write is refused with question.annulled.
    [Fact]
    public void AnAnnulledQuestion_RefusesUpdatePublishUnpublishAndDelete_WithAnnulled()
    {
        var (question, latest) = Published();
        question.Annul(latest, "Anulada.").IsSuccess.Should().BeTrue();

        var results = new[]
        {
            question.Update(SingleChoice("<p>Outro enunciado.</p>")),
            question.Publish(),
            question.Unpublish(),
            question.EnsureDeletable()
        };

        results.Should().OnlyContain(result => result.IsFailure
            && result.Error!.Code == QuestionBankErrorCodes.QuestionAnnulled
            && result.Error.Kind == ErrorKind.Conflict);
        question.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        question.Status.Should().Be(QuestionStatus.Annulled);
    }

    // AC5, BR6: reinstating is the next version again, published, with the reason as its note.
    [Fact]
    public void Reinstate_AnAnnulledQuestion_MakesTheNextVersionPublished_WithTheReasonAsNote()
    {
        var (question, latest) = Published();
        var annulled = question.Annul(latest, "Anulada.").Value!;

        var result = question.Reinstate(annulled, "  A banca reverteu.  ");

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        var version = result.Value!;
        version.Number.Should().Be(3);
        version.Annulled.Should().BeFalse();
        version.AnswerKeyChanged.Should().BeFalse();
        version.Reason.Should().Be("A banca reverteu.");
        version.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        version.Options.Select(option => option.IsCorrect).Should().Equal(true, false);
        annulled.Annulled.Should().BeTrue("the annulment version is never touched (BR2)");
    }

    [Fact]
    public void AReinstatedQuestion_CanBeEditedAgain_AndTheEditMakesTheNextVersion()
    {
        var (question, latest) = Published();
        var annulled = question.Annul(latest, "Anulada.").Value!;
        var reinstated = question.Reinstate(annulled, "Revertida.").Value!;

        question.Update(SingleChoice("<p>Qual é a capital da Espanha?</p>")).IsSuccess.Should().BeTrue();
        var edit = question.NextVersion(reinstated, null);

        edit.IsSuccess.Should().BeTrue();
        edit.Value!.Number.Should().Be(4);
        edit.Value.Annulled.Should().BeFalse();
        edit.Value.StatementHtml.Should().Be("<p>Qual é a capital da Espanha?</p>");
    }

    // AC6, BR6, BR3: only an annulled question is reinstated and the reason rules are the same.
    [Fact]
    public void Reinstate_AQuestionThatIsNotAnnulled_IsRefusedWithNotAnnulled()
    {
        var (published, latest) = Published();
        var draft = Question.Create(SingleChoice()).Value;

        var fromPublished = published.Reinstate(latest, "Revertida.");
        var fromDraft = draft.Reinstate(null, "Revertida.");

        new[] { fromPublished, fromDraft }.Should().OnlyContain(result => result.IsFailure
            && result.Error!.Code == QuestionBankErrorCodes.QuestionNotAnnulled
            && result.Error.Kind == ErrorKind.Conflict);
        published.Status.Should().Be(QuestionStatus.Published);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Reinstate_WithAMissingOrBlankReason_IsRefused_AndTheQuestionStaysAnnulled(string? reason)
    {
        var (question, latest) = Published();
        var annulled = question.Annul(latest, "Anulada.").Value!;

        var result = question.Reinstate(annulled, reason);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatusReasonRequired);
        question.Status.Should().Be(QuestionStatus.Annulled);
    }

    [Fact]
    public void Reinstate_AReasonOverTheLimit_IsRefusedWithStatusReasonTooLong()
    {
        var (question, latest) = Published();
        var annulled = question.Annul(latest, "Anulada.").Value!;

        var result = question.Reinstate(annulled, new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(QuestionBankErrorCodes.QuestionStatusReasonTooLong);
        question.Status.Should().Be(QuestionStatus.Annulled);
    }
}
