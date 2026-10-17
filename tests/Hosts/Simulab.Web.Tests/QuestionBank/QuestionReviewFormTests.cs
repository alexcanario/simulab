using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 `/admin/questions/{id}` as far as bUnit reaches (AC14, UC1, UC2, UC6): Send for review beside Publish,
/// the frozen form of a question in review with only Withdraw to leave, the rejection alert and the review
/// history card. The rules are the Api's and have their own tests.
/// </summary>
public sealed class QuestionReviewFormTests : QuestionPageTestContext
{
    private const string PublicationCard = "section[aria-labelledby='question-section-publication-title']";
    private const string ReviewCard = "section[aria-labelledby='question-section-review-history-title']";

    private static readonly DateTimeOffset SentAt = new(2026, 10, 16, 14, 2, 0, TimeSpan.Zero);

    /// <summary>A draft the checklist accepts: one correct option.</summary>
    private static readonly QuestionResponse CompleteDraft = Draft with
    {
        Options = [new(0, "<p>Fui <strong>à</strong> escola.</p>", true), new(1, "<p>Fui a escola.</p>", false)]
    };

    private static readonly QuestionResponse InReview = CompleteDraft with
    {
        Status = QuestionStatus.InReview,
        SubmittedAt = SentAt
    };

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));

    private static string[] Row(AngleSharp.Dom.IElement row) =>
        [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())];

    // AC14, UC1: a draft shows Send for review beside Publish; Save stays the page's one filled button.
    [Fact]
    public void Draft_OffersSendForReviewBesidePublish()
    {
        Api.Questions.Add(CompleteDraft);

        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-send-for-review").Should().ContainSingle());
        page.Find("#question-send-for-review").TextContent.Should().Contain("Send for review");
        page.Find("#question-send-for-review").HasAttribute("disabled").Should().BeFalse();
        page.FindAll("#question-publish").Should().ContainSingle();
        page.FindAll("#question-withdraw").Should().BeEmpty();
    }

    // AC14: Send is disabled for the same reason as Publish, in the same order, and the one reason line describes both.
    [Fact]
    public void Draft_Incomplete_SendIsDisabledForTheSameReasonAsPublish()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-send-for-review").Should().ContainSingle());
        page.Find("#question-send-for-review").HasAttribute("disabled").Should().BeTrue();
        page.Find("#question-publish").HasAttribute("disabled").Should().BeTrue();
        var reason = page.Find("#question-publication-reason").TextContent;
        reason.Should().NotBeNullOrWhiteSpace();
        page.Find("#question-send-for-review").GetAttribute("aria-describedby").Should().Be("question-publication-reason");
        page.Find("#question-publish").GetAttribute("aria-describedby").Should().Be("question-publication-reason");
    }

    // AC14, UC1: sending calls the Api, the card switches to the review state and the snackbar says it.
    [Fact]
    public void Send_CallsTheApi_AndTheCardShowsTheQuestionInReview_WithOnlyWithdrawToLeave()
    {
        var providers = RenderProviders();
        Api.Questions.Add(CompleteDraft);
        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-send-for-review");

        page.Find("#question-send-for-review").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{CompleteDraft.Id}/submit"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question sent for review."));
        page.WaitForAssertion(() => page.FindAll("#question-in-review-alert").Should().ContainSingle());
        page.Find("#question-in-review-alert").TextContent.Should().Contain("This question is in review and cannot be edited.");
        page.Find(PublicationCard).TextContent.Should().Contain("In review").And.Contain("Waiting for a reviewer since");
        page.FindAll("#question-withdraw").Should().ContainSingle();
        page.FindAll("#question-preview").Should().ContainSingle("the preview is a read and stays");
        page.FindAll("#question-publish, #question-send-for-review, #question-unpublish, #question-annul, .app-form-save")
            .Should().BeEmpty();
        RichText.Editors.Values.Should().OnlyContain(editor => editor.Disabled);
    }

    // AC14: a question that opens in review is frozen the same way, with the time of the send.
    [Fact]
    public void AQuestionInReview_OpensFrozen_WithTheSendTimeAndTheStatusChipInTheWarningTone()
    {
        Api.Questions.Add(InReview);
        Api.Reviews[InReview.Id] = [new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt)];

        var page = RenderEdit(InReview.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-in-review-alert").Should().ContainSingle());
        page.Find(PublicationCard).TextContent.Should().Contain(SentAt.ToString("g", System.Globalization.CultureInfo.CurrentCulture));
        page.FindAll($"{PublicationCard} .app-status-chip-warning").Should().ContainSingle();
        RichText.Editors.Values.Should().OnlyContain(editor => editor.Disabled);
        page.FindAll("#question-annulled-alert").Should().BeEmpty();
    }

    // AC14, UC2: withdrawing asks nothing, calls the Api, and the form is editable again.
    [Fact]
    public void Withdraw_AsksNoReason_CallsTheApi_AndTheFormIsEditableAgain()
    {
        var providers = RenderProviders();
        Api.Questions.Add(InReview);
        Api.Reviews[InReview.Id] = [new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt)];
        var page = RenderEdit(InReview.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-withdraw");

        page.Find("#question-withdraw").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{InReview.Id}/withdraw"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question withdrawn from review. It is a draft again."));
        providers.Dialogs.FindAll("#question-status-reason").Should().BeEmpty("withdrawing is reversible and asks nothing");
        page.WaitForAssertion(() => page.FindAll("#question-in-review-alert").Should().BeEmpty());
        page.FindAll(".app-form-save").Should().ContainSingle();
        page.FindAll("#question-send-for-review").Should().ContainSingle();
        RichText.Editors["question-statement"].Disabled.Should().BeFalse();
    }

    // AC14: a withdrawal refused because a reviewer decided meanwhile shows the Api's words in the card's alert.
    [Fact]
    public void Withdraw_WhenTheQuestionIsNoLongerInReview_ShowsTheApisTextInTheCard()
    {
        Api.Questions.Add(InReview);
        Api.ReviewFailure = (System.Net.HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotInReview);
        var page = RenderEdit(InReview.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-withdraw");

        page.Find("#question-withdraw").Click();

        page.WaitForAssertion(() => page.Find(PublicationCard).TextContent.Should().Contain("This question is not in review."));
    }

    // AC14, UC6: a rejected draft says why, in the card, with the date.
    [Fact]
    public void ARejectedDraft_ShowsTheRejectionAlert_WithTheDateAndTheReason()
    {
        var rejectedAt = SentAt.AddDays(1);
        Api.Questions.Add(CompleteDraft);
        Api.Reviews[CompleteDraft.Id] =
        [
            new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt),
            new QuestionReviewEntryResponse(QuestionReviewKind.Rejected, "A chave da C contradiz a explicação.", rejectedAt)
        ];

        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-rejected-alert").Should().ContainSingle());
        var text = page.Find("#question-rejected-alert").TextContent;
        text.Should().Contain("Rejected on").And.Contain("A chave da C contradiz a explicação.")
            .And.Contain(rejectedAt.ToString("g", System.Globalization.CultureInfo.CurrentCulture));
    }

    // AC14: the alert goes with the next send, because the latest entry is no longer the rejection.
    [Fact]
    public void ARejectedDraft_SentAgain_NoLongerShowsTheRejectionAlert()
    {
        Api.Questions.Add(CompleteDraft);
        Api.Reviews[CompleteDraft.Id] =
        [
            new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt),
            new QuestionReviewEntryResponse(QuestionReviewKind.Rejected, "Revise a chave.", SentAt.AddDays(1))
        ];
        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-rejected-alert");

        page.Find("#question-send-for-review").Click();

        page.WaitForAssertion(() => page.FindAll("#question-rejected-alert").Should().BeEmpty());
    }

    // UC6, BR7: the review history card lists the entries newest first, with the kind, the tone and the reason.
    [Fact]
    public void ReviewHistory_ListsTheEntriesNewestFirst_WithTheirKindAndReason()
    {
        Api.Questions.Add(CompleteDraft);
        Api.Reviews[CompleteDraft.Id] =
        [
            new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt),
            new QuestionReviewEntryResponse(QuestionReviewKind.Rejected, "Revise a chave.", SentAt.AddDays(1)),
            new QuestionReviewEntryResponse(QuestionReviewKind.Submitted, null, SentAt.AddDays(2))
        ];

        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);

        var card = page.WaitForElement(ReviewCard);
        page.WaitForAssertion(() => card.QuerySelectorAll("tbody tr").Should().HaveCount(3));
        var rows = card.QuerySelectorAll("tbody tr").Select(Row).ToList();
        rows[0].Should().Contain("Sent");
        rows[1].Should().Contain("Rejected").And.Contain("Revise a chave.");
        rows[2].Should().Contain("Sent");
        page.FindAll($"{ReviewCard} .app-status-chip-error").Should().ContainSingle("a rejection is the refusal tone");
        page.FindAll($"{ReviewCard} .app-status-chip-info").Should().HaveCount(2, "a send is the information tone");
        card.TextContent.Should().Contain("Review history");
    }

    // BR7: without entries there is no card.
    [Fact]
    public void ReviewHistory_WithoutEntries_HasNoCard()
    {
        Api.Questions.Add(CompleteDraft);

        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-send-for-review").Should().ContainSingle());
        page.FindAll(ReviewCard).Should().BeEmpty();
    }

    // The failure is shown, never silent: the card holds the error state with Try again.
    [Fact]
    public void ReviewHistory_WhenTheEntriesFailToLoad_ShowsTheErrorStateInTheCard()
    {
        Api.Questions.Add(CompleteDraft);
        Api.ReviewsReadFailure = (System.Net.HttpStatusCode.InternalServerError, "common.unexpected_error");

        var page = RenderEdit(CompleteDraft.Id);
        WaitForEditors(page);

        var card = page.WaitForElement(ReviewCard);
        card.QuerySelectorAll("button").Select(button => button.TextContent.Trim()).Should().Contain(text => text.Contains("Try again", StringComparison.Ordinal));
    }
}
