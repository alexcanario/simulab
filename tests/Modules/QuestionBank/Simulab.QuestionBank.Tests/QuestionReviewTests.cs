using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-105, the review rules without a database: sending (BR1, BR2), the frozen state (BR3), withdrawing (BR4),
/// approving (BR5) and rejecting (BR6), each with the entry it records (BR7).
/// </summary>
public sealed class QuestionReviewTests
{
    private static readonly Guid Topic = Guid.CreateVersion7();

    private static SanitizedHtml Text(string html) => new(html, html);

    private static QuestionContent Complete(string statement = "<p>Qual é a capital da França?</p>") =>
        new(
            Topic,
            "pt-BR",
            Text(statement),
            null,
            Text("<p>É Paris.</p>"),
            3,
            [new OptionDraft(Text("Paris"), true), new OptionDraft(Text("Lyon"), false)]);

    private static QuestionContent NoKey() =>
        new(
            Topic,
            "pt-BR",
            Text("<p>Sem chave.</p>"),
            null,
            SanitizedHtml.Empty,
            null,
            [new OptionDraft(Text("Paris"), false), new OptionDraft(Text("Lyon"), false)]);

    private static Question Draft(QuestionContent? content = null) => Question.Create(content ?? Complete()).Value!;

    private static Question InReview()
    {
        var question = Draft();
        question.Submit().IsSuccess.Should().BeTrue();

        return question;
    }

    private static Question Published()
    {
        var question = Draft();
        question.Publish().IsSuccess.Should().BeTrue();

        return question;
    }

    private static Question Annulled()
    {
        var question = Published();
        var first = question.NextVersion(null, null);
        question.Annul(first.Value, "A banca anulou.").IsSuccess.Should().BeTrue();

        return question;
    }

    private static void AssertRefused<T>(Result<T> result, string code, ErrorKind kind)
    {
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(code);
        result.Error.Kind.Should().Be(kind);
    }

    private static void AssertRefused(Result result, string code, ErrorKind kind)
    {
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(code);
        result.Error.Kind.Should().Be(kind);
    }

    // AC1, BR1, BR2, BR7: a complete draft goes in review and one Submitted entry records it; no version is made.
    [Fact]
    public void Submit_ACompleteDraft_BecomesInReview_WithOneSubmittedEntryAndNoVersion()
    {
        var question = Draft();

        var result = question.Submit();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.InReview);
        var entry = result.Value!;
        entry.Kind.Should().Be(QuestionReviewKind.Submitted);
        entry.QuestionId.Should().Be(question.Id);
        entry.Reason.Should().BeNull();
        question.NextVersion(null, null).Value.Should().BeNull("versions belong to published content (BR2)");
    }

    // AC2, BR2: the completeness check is the one publishing runs, with its codes, and nothing changes.
    [Fact]
    public void Submit_AnIncompleteDraft_IsRefusedWithThePublishCode_AndStaysADraft()
    {
        var question = Draft(NoKey());

        var result = question.Submit();

        AssertRefused(result, QuestionBankErrorCodes.QuestionKeyInvalid, ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.Draft);
    }

    // AC2: the state is judged first, as a publish judges it.
    [Fact]
    public void Submit_InReviewPublishedOrAnnulled_IsRefusedWithTheCodeOfThatState_AndNothingChanges()
    {
        var inReview = InReview();
        var published = Published();
        var annulled = Annulled();

        AssertRefused(inReview.Submit(), QuestionBankErrorCodes.QuestionAlreadyInReview, ErrorKind.Conflict);
        AssertRefused(published.Submit(), QuestionBankErrorCodes.QuestionAlreadyPublished, ErrorKind.Conflict);
        AssertRefused(annulled.Submit(), QuestionBankErrorCodes.QuestionAnnulled, ErrorKind.Conflict);

        inReview.Status.Should().Be(QuestionStatus.InReview);
        published.Status.Should().Be(QuestionStatus.Published);
        annulled.Status.Should().Be(QuestionStatus.Annulled);
    }

    // AC3, BR3: save, publish, unpublish and delete answer question.in_review and change nothing.
    [Fact]
    public void InReview_RefusesUpdatePublishUnpublishAndDelete_WithInReview_AndKeepsItsContent()
    {
        var question = InReview();

        AssertRefused(question.Update(Complete("<p>Outro enunciado.</p>")), QuestionBankErrorCodes.QuestionInReview, ErrorKind.Conflict);
        AssertRefused(question.Publish(), QuestionBankErrorCodes.QuestionInReview, ErrorKind.Conflict);
        AssertRefused(question.Unpublish(), QuestionBankErrorCodes.QuestionInReview, ErrorKind.Conflict);
        AssertRefused(question.EnsureDeletable(), QuestionBankErrorCodes.QuestionInReview, ErrorKind.Conflict);

        question.Status.Should().Be(QuestionStatus.InReview);
        question.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
    }

    // AC3, BR3: annul keeps its own rule for a question in review.
    [Fact]
    public void Annul_AQuestionInReview_KeepsItsOwnCode_NotPublished()
    {
        var question = InReview();

        var result = question.Annul(null, "Anulada.");

        AssertRefused(result, QuestionBankErrorCodes.QuestionNotPublished, ErrorKind.Conflict);
        question.Status.Should().Be(QuestionStatus.InReview);
    }

    // AC4, BR4: the question is a draft again with the same content, and a Withdrawn entry records it.
    [Fact]
    public void Withdraw_AQuestionInReview_BecomesADraftAgain_WithAWithdrawnEntry()
    {
        var question = InReview();

        var result = question.Withdraw();

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Draft);
        question.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        result.Value!.Kind.Should().Be(QuestionReviewKind.Withdrawn);
        result.Value.QuestionId.Should().Be(question.Id);
        result.Value.Reason.Should().BeNull();
    }

    [Fact]
    public void Withdraw_ADraftPublishedOrAnnulledQuestion_IsRefusedWithNotInReview()
    {
        AssertRefused(Draft().Withdraw(), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
        AssertRefused(Published().Withdraw(), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
        AssertRefused(Annulled().Withdraw(), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
    }

    // AC5, BR5: the question is published, the version F-98 makes is version 1, and the note is trimmed.
    [Fact]
    public void Approve_AQuestionInReview_PublishesIt_WithTheTrimmedNote_AndVersionOneHoldsTheContent()
    {
        var question = InReview();

        var result = question.Approve("  Conferida com o edital.  ");

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Published);
        result.Value!.Kind.Should().Be(QuestionReviewKind.Approved);
        result.Value.Reason.Should().Be("Conferida com o edital.");
        var version = question.NextVersion(null, null).Value;
        version.Should().NotBeNull();
        version!.Number.Should().Be(1);
        version.StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_WithNoNote_PublishesIt_AndTheEntryHoldsNoReason(string? note)
    {
        var question = InReview();

        var result = question.Approve(note);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Reason.Should().BeNull();
        question.Status.Should().Be(QuestionStatus.Published);
    }

    // AC5: 500 characters once trimmed is accepted; 501 is refused and the question stays in review.
    [Fact]
    public void Approve_ANoteOf500Characters_IsAccepted_And501AreRefused_LeavingItInReview()
    {
        var accepted = InReview();
        var refused = InReview();

        accepted.Approve(" " + new string('a', QuestionLimits.ChangeReasonMaxLength) + " ").IsSuccess.Should().BeTrue();
        var result = refused.Approve(new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        AssertRefused(result, QuestionBankErrorCodes.QuestionReviewReasonTooLong, ErrorKind.Validation);
        refused.Status.Should().Be(QuestionStatus.InReview);
    }

    // AC6, BR6: a Rejected entry holds the trimmed reason and the question is a draft.
    [Fact]
    public void Reject_AQuestionInReview_BecomesADraft_WithTheTrimmedReason()
    {
        var question = InReview();

        var result = question.Reject("  A chave da C contradiz a explicação.  ");

        result.IsSuccess.Should().BeTrue();
        question.Status.Should().Be(QuestionStatus.Draft);
        result.Value!.Kind.Should().Be(QuestionReviewKind.Rejected);
        result.Value.Reason.Should().Be("A chave da C contradiz a explicação.");
        result.Value.QuestionId.Should().Be(question.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithAMissingOrBlankReason_IsRefused_AndItStaysInReview(string? reason)
    {
        var question = InReview();

        var result = question.Reject(reason);

        AssertRefused(result, QuestionBankErrorCodes.QuestionReviewReasonRequired, ErrorKind.Validation);
        question.Status.Should().Be(QuestionStatus.InReview);
    }

    [Fact]
    public void Reject_AReasonOf500Characters_IsAccepted_And501AreRefused_LeavingItInReview()
    {
        var accepted = InReview();
        var refused = InReview();

        accepted.Reject(new string('a', QuestionLimits.ChangeReasonMaxLength)).IsSuccess.Should().BeTrue();
        var result = refused.Reject(new string('a', QuestionLimits.ChangeReasonMaxLength + 1));

        AssertRefused(result, QuestionBankErrorCodes.QuestionReviewReasonTooLong, ErrorKind.Validation);
        refused.Status.Should().Be(QuestionStatus.InReview);
    }

    // AC7, BR6: the state is judged before the reason.
    [Fact]
    public void ApproveAndReject_AQuestionNotInReview_AreRefusedWithNotInReview_BeforeTheReasonIsLooked_At()
    {
        var draft = Draft();
        var published = Published();

        AssertRefused(draft.Approve(null), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
        AssertRefused(published.Approve(new string('a', 501)), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
        AssertRefused(draft.Reject(null), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);
        AssertRefused(Annulled().Reject("Motivo."), QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);

        draft.Status.Should().Be(QuestionStatus.Draft);
        published.Status.Should().Be(QuestionStatus.Published);
    }
}
