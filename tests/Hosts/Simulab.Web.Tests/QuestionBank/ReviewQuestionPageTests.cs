using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-105 `/admin/questions/review/{id}` (AC15, AC10 screen part, UC4, UC5, BR9): the question as a student sees it with
/// the key always shown, Approve with and without a note, Reject with its required reason, the conflict, and every state
/// the screen design lists. The rules themselves are the Api's and have their own tests.
/// </summary>
public sealed class ReviewQuestionPageTests : QuestionPageTestContext
{
    private const string ReasonId = "question-status-reason";
    private const string QueueRoute = "/admin/questions/review";

    private static readonly DateTimeOffset SentAt = new(2026, 10, 16, 14, 2, 0, TimeSpan.Zero);

    /// <summary>A single choice question in review: a base text, a key (option A), an explanation, from a past edition.</summary>
    private static readonly QuestionResponse ToReview = Published with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000050"),
        Status = QuestionStatus.InReview,
        CurrentVersion = null,
        SubmittedAt = SentAt,
        Origin = FromGuard.Origin
    };

    private IRenderedComponent<ReviewQuestion> RenderReview(Guid id) =>
        Render<ReviewQuestion>(parameters => parameters.Add(page => page.Id, id));

    private IRenderedComponent<ReviewQuestion> RenderReady(QuestionResponse question)
    {
        Api.Questions.Add(question);
        var page = RenderReview(question.Id);
        page.WaitForElement("#review-approve");

        return page;
    }

    private string Location => Services.GetRequiredService<NavigationManager>().Uri;

    private static Dictionary<string, string> AsideOf(IRenderedComponent<ReviewQuestion> page) =>
        page.FindAll(".app-form-aside-row").ToDictionary(
            row => row.QuerySelector(".app-form-aside-row-label")!.TextContent.Trim(),
            row => row.QuerySelector(".app-form-aside-row-value")!.TextContent.Trim());

    private static Task TypeAsync(IRenderedComponent<MudDialogProvider> dialogs, string text) =>
        dialogs.InvokeAsync(() => dialogs.FindComponents<AppTextField<string>>()
            .Single(field => field.Instance.Id == ReasonId)
            .Instance.ValueChanged.InvokeAsync(text));

    private static string DecisionPath(Guid id, string decision) => $"/api/v1/question-bank/review-queue/{id}/{decision}";

    // The page renders with its real parameter and reads the review route, never the editor's.
    [Fact]
    public void Render_ShowsTheTitleTheBreadcrumbsAndReadsTheQuestionFromTheReviewRoute()
    {
        var page = RenderReady(ToReview);

        page.Find("h1").TextContent.Should().Be("Review question");
        var crumbs = page.Find(".app-breadcrumbs");
        crumbs.TextContent.Should().Contain("Content").And.Contain("Review queue").And.Contain("Review question");
        crumbs.QuerySelectorAll("a[href]").Select(link => link.GetAttribute("href"))
            .Should().Contain(QueueRoute, "the breadcrumb is the way back");
        Api.Received.Should().Contain(call => call.Method == HttpMethod.Get && call.Path == $"/api/v1/question-bank/review-queue/{ToReview.Id}");
        Api.Received.Should().NotContain(call => call.Path == $"/api/v1/question-bank/questions/{ToReview.Id}");
    }

    // BR9: the question as the student sees it, and the key, the explanation and the classification beside it.
    [Fact]
    public void Ready_ShowsTheQuestionWithTheKeyAndTheExplanationAlwaysShown()
    {
        var page = RenderReady(ToReview);

        page.Find("#question-preview-statement").TextContent.Should().Contain("Onde entra a vírgula?");
        page.Find("#question-preview-base-text").TextContent.Should().Contain("Texto-base com");
        page.Find("#question-preview-source").TextContent.Should().Contain("Guarda Municipal").And.Contain("Question 12");
        page.Find(".app-question-paper").GetAttribute("lang").Should().Be("pt-PT");

        // The key is icon, text and border: one option is marked and says so in words.
        var marked = page.FindAll(".app-question-option.is-correct");
        marked.Should().ContainSingle();
        marked[0].TextContent.Should().Contain("Antes do vocativo").And.Contain("Correct answer");
        page.FindAll(".app-question-option").Should().HaveCount(3);

        // The answer section is there from the start: no Show/Hide button.
        page.FindAll("#question-preview-toggle").Should().BeEmpty();
        page.Find("#question-preview-key").TextContent.Trim().Should().Be("A", "the Answer heading carries the key in words");
        var answer = page.Find("section.app-preview-answer");
        answer.TextContent.Should().Contain("Answer").And.Contain("Explanation");
        page.Find("#question-preview-explanation").TextContent.Should().Contain("Porque separa o vocativo.");
        page.Find("#question-preview-explanation").GetAttribute("lang").Should().Be("pt-PT");
    }

    [Fact]
    public void Ready_TheAsideListsTheClassificationAndTheSendTime()
    {
        var page = RenderReady(ToReview);

        page.Find("aside.app-form-aside").GetAttribute("aria-labelledby").Should().NotBeNullOrEmpty();
        page.Find(".app-form-aside-title").TextContent.Should().Be("Summary");
        var rows = AsideOf(page);
        rows["Status"].Should().Be("In review");
        rows["Subject"].Should().Be("Português");
        rows["Topic"].Should().Be("Pontuação");
        rows["Content language"].Should().Contain("Português");
        rows["Difficulty"].Should().Be("Not filled");
        rows["Type"].Should().Be("Single choice");
        rows["Source"].Should().Contain("Guarda Municipal").And.Contain("Question 12");
        rows["Sent on"].Should().Be(SentAt.ToString("g", System.Globalization.CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Ready_AnAuthoredQuestion_SaysAuthoredAsItsSource_AndShowsNoSourceLine()
    {
        var page = RenderReady(ToReview with { Origin = null, Difficulty = 4 });

        AsideOf(page)["Source"].Should().Be("Authored");
        AsideOf(page)["Difficulty"].Should().Be("4");
        page.FindAll("#question-preview-source").Should().BeEmpty();
    }

    // Nothing here edits content: no field, no link to the editor, and one filled button, Approve.
    [Fact]
    public void Ready_ReadsOnly_NoFieldNoLinkToTheEditor_AndApproveIsTheOneFilledButton()
    {
        var page = RenderReady(ToReview);

        page.FindAll("input, textarea, select, [contenteditable='true']").Should().BeEmpty();
        page.FindAll("a[href]").Select(link => link.GetAttribute("href"))
            .Should().NotContain($"/admin/questions/{ToReview.Id}");
        page.FindAll("#review-question a[href]").Should().BeEmpty("only links inside the question's own content could appear");

        page.Find("#review-reject").ClassName.Should().Contain("mud-button-outlined");
        page.Find("#review-reject").TextContent.Should().Contain("Reject");
        page.Find("#review-approve").ClassName.Should().Contain("mud-button-filled");
        page.Find("#review-approve").TextContent.Should().Contain("Approve");
        page.FindAll("button.mud-button-filled").Should().ContainSingle();
        page.Find(".app-form-actions").QuerySelectorAll("button").Select(button => button.Id)
            .Should().Equal("review-reject", "review-approve");
    }

    [Fact]
    public void Ready_TrueFalse_ShowsBothAnswersWithTheKeyMarked()
    {
        var page = RenderReady(ToReview with { Type = QuestionType.TrueFalse, TrueFalseKey = true, Options = [] });

        page.FindAll("#question-preview-true-false li").Should().HaveCount(2);
        var marked = page.FindAll("#question-preview-true-false .is-correct");
        marked.Should().ContainSingle();
        marked[0].TextContent.Should().Contain("True").And.Contain("Correct answer");
        AsideOf(page)["Type"].Should().Be("True/false");
    }

    [Fact]
    public void Ready_MultipleAnswer_SaysHowManyToMarkAndMarksEveryCorrectOption()
    {
        var multiple = ToReview with
        {
            Type = QuestionType.MultipleAnswer,
            Options = [new(0, "<p>Um</p>", true), new(1, "<p>Dois</p>", false), new(2, "<p>Três</p>", true)]
        };

        var page = RenderReady(multiple);

        page.Find("#question-preview-select-count").TextContent.Should().Be("Mark 2 options.");
        page.FindAll(".app-question-option.is-correct").Should().HaveCount(2);
    }

    // Long text: a long option, a long unbroken URL and a long explanation sit in the kit's wrapping element, with no
    // fixed width and no truncation (bUnit cannot measure layout; the gallery shows the real wrap).
    [Fact]
    public void Ready_LongText_SitsInTheContentElementThatWraps()
    {
        var longOption = string.Join(' ', Enumerable.Repeat("alternativa", 80));
        var longUrl = "https://exemplo.com.br/" + new string('a', 300);
        var longExplanation = string.Join(' ', Enumerable.Repeat("explicação", 120));
        var page = RenderReady(ToReview with
        {
            Options = [new(0, $"<p>{longOption}</p>", true), new(1, $"<p>{longUrl}</p>", false)],
            ExplanationHtml = $"<p>{longExplanation}</p>"
        });

        var option = page.FindAll(".app-question-option .app-question-content")[0];
        option.TextContent.Should().Be(longOption);
        option.GetAttribute("style").Should().BeNull();
        page.FindAll(".app-question-option .app-question-content")[1].TextContent.Should().Be(longUrl);
        var explanation = page.Find("#question-preview-explanation");
        explanation.TextContent.Should().Be(longExplanation);
        explanation.ClassName.Should().Contain("app-question-content");
        explanation.GetAttribute("style").Should().BeNull();
        page.FindAll("#review-question .app-truncate").Should().BeEmpty("nothing in the card is cut with an ellipsis");
    }

    // AC15: Approve without a note publishes, sends no note and the send the reviewer saw, and returns to the queue.
    [Fact]
    public void Approve_WithoutANote_SendsNoNoteAndTheSendSeen_ThenGoesToTheQueueWithTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);

        page.Find("#review-approve").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().ContainSingle());
        providers.Dialogs.Markup.Should().Contain("Approve this question?")
            .And.Contain("It is published at once and practice can use it.")
            .And.Contain("Optional. Plain text, up to 500 characters.")
            .And.Contain("0/500");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == DecisionPath(ToReview.Id, "approve")));
        var request = Api.LastApproval();
        request.Note.Should().BeNull();
        request.SubmittedAt.Should().Be(SentAt);
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question approved and published."));
        page.WaitForAssertion(() => Location.Should().EndWith(QueueRoute));
        Api.Questions.Single().Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Approve_WithANote_SendsItTrimmed()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        page.Find("#review-approve").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        await TypeAsync(providers.Dialogs, "  Boa questão.  ");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Path == DecisionPath(ToReview.Id, "approve")));
        Api.LastApproval().Note.Should().Be("Boa questão.");
        page.WaitForAssertion(() => Location.Should().EndWith(QueueRoute));
    }

    // AC15: Reject needs a reason; an empty one is refused in the dialog, with the Api's words, before any call.
    [Fact]
    public void Reject_WithAnEmptyReason_IsRefusedInTheDialog_AndNothingIsSent()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);

        page.Find("#review-reject").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().ContainSingle());
        providers.Dialogs.Markup.Should().Contain("Reject this question?").And.Contain("Required. Plain text, up to 500 characters.");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Write the reason for the rejection."));
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/reject", StringComparison.Ordinal));
        Location.Should().NotEndWith(QueueRoute);
        page.FindAll("#review-approve").Should().ContainSingle("the question is still on screen");
    }

    [Fact]
    public async Task Reject_WithAReason_SendsItTrimmed_ThenGoesToTheQueueWithTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        page.Find("#review-reject").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        await TypeAsync(providers.Dialogs, "  A chave da alternativa C contradiz a explicação.  ");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == DecisionPath(ToReview.Id, "reject")));
        var request = Api.LastRejection();
        request.Reason.Should().Be("A chave da alternativa C contradiz a explicação.");
        request.SubmittedAt.Should().Be(SentAt);
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question rejected. It is a draft again."));
        page.WaitForAssertion(() => Location.Should().EndWith(QueueRoute));
        Api.Questions.Single().Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public void Dialog_Cancelled_SendsNothing_AndTheQuestionStaysForTheReviewer()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        page.Find("#review-reject").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        providers.Dialogs.Find("#question-status-reason-cancel").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().BeEmpty());
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
        page.WaitForAssertion(() => page.FindAll("#review-approve").Should().ContainSingle());
        Location.Should().NotEndWith(QueueRoute);
    }

    // AC15: a question decided meanwhile. The Api's 409 is read in the dialog, where the reviewer is typing; when the
    // dialog closes the page reads the question again and, being out of review, turns into the "no longer" card.
    [Fact]
    public async Task Reject_WhenAnotherReviewerDecidedFirst_ShowsTheConflictInTheDialog_ThenTheNoLongerInReviewCard()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        Api.DecisionFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);
        page.Find("#review-reject").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        await TypeAsync(providers.Dialogs, "Motivo.");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Reload the page and try again."));
        Location.Should().NotEndWith(QueueRoute);
        providers.Snackbars.Markup.Should().NotContain("Question rejected.");

        // Meanwhile the other reviewer's decision is stored: the question is a draft again.
        Api.Questions[0] = ToReview with { Status = QuestionStatus.Draft, SubmittedAt = null };
        providers.Dialogs.Find("#question-status-reason-cancel").Click();

        page.WaitForAssertion(() => page.FindAll("#review-gone").Should().ContainSingle());
        page.Find("#review-gone").TextContent.Should()
            .Contain("This question is no longer in review: another reviewer decided it, or it was withdrawn.");
        page.Find("#review-gone a.app-link").GetAttribute("href").Should().Be(QueueRoute);
        page.Find("#review-gone a.app-link").TextContent.Should().Be("Back to the queue");
        page.FindAll("#review-approve, #review-reject, #review-question").Should().BeEmpty();
        Api.ReviewReads.Should().Be(2, "the first load and the re-read after the dialog closed");
    }

    // A conflict that left the question in review (a re-send, say): the page reads it again and shows it again.
    [Fact]
    public async Task Approve_RefusedWithAConflict_ButStillInReview_ShowsTheQuestionAgainAfterTheDialogCloses()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        Api.DecisionFailure = (HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);
        page.Find("#review-approve").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");
        await TypeAsync(providers.Dialogs, "Nota.");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Reload the page and try again."));

        providers.Dialogs.Find("#question-status-reason-cancel").Click();

        page.WaitForAssertion(() => Api.ReviewReads.Should().Be(2));
        page.WaitForAssertion(() => page.FindAll("#review-approve").Should().ContainSingle());
        page.FindAll("#review-gone").Should().BeEmpty();
    }

    // The key needs a reason when the answer key changed: the Api's refusal is shown under the note, not above it.
    [Fact]
    public void Approve_RefusedBecauseTheKeyChanged_ShowsTheCodesTextUnderTheNote_NotAboveIt()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        Api.DecisionFailure = (HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        page.Find("#review-approve").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Write the reason for the answer key change."));
        providers.Dialogs.FindAll(".app-alert").Should().BeEmpty("a refusal about the note sits under the field");
        Location.Should().NotEndWith(QueueRoute);
    }

    // While the decision is in flight both page buttons and both dialog buttons are disabled.
    [Fact]
    public async Task Decision_InFlight_DisablesBothButtonsOfThePageAndOfTheDialog()
    {
        var providers = RenderProviders();
        var page = RenderReady(ToReview);
        var gate = new TaskCompletionSource();
        Api.WriteGate = gate;
        page.Find("#review-reject").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");
        await TypeAsync(providers.Dialogs, "Motivo.");

        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() =>
        {
            page.Find("#review-reject").HasAttribute("disabled").Should().BeTrue();
            page.Find("#review-approve").HasAttribute("disabled").Should().BeTrue();
        });
        providers.Dialogs.Find("#question-status-reason-confirm").HasAttribute("disabled").Should().BeTrue();
        providers.Dialogs.Find("#question-status-reason-cancel").HasAttribute("disabled").Should().BeTrue();
        providers.Dialogs.FindAll(".mud-progress-circular").Should().NotBeEmpty("the confirm button shows the progress");

        gate.SetResult();

        page.WaitForAssertion(() => Location.Should().EndWith(QueueRoute));
    }

    [Fact]
    public void NotFound_ShowsTheCodesTextAndTheBackLink()
    {
        var page = RenderReview(Guid.Parse("0198f5a0-0000-7000-8000-0000000000ff"));

        page.WaitForAssertion(() => page.FindAll("#review-not-found").Should().ContainSingle());
        page.Find("#review-not-found").TextContent.Should().Contain("This question no longer exists.");
        page.Find("#review-not-found a.app-link").GetAttribute("href").Should().Be(QueueRoute);
        page.Find("#review-not-found a.app-link").TextContent.Should().Be("Back to the queue");
        page.FindAll("#review-approve, #review-reject").Should().BeEmpty();
    }

    // The Api's 409 on the first load: a question that is not in review (decided, withdrawn) is the same card.
    [Fact]
    public void FirstLoad_OfAQuestionNotInReview_ShowsTheNoLongerInReviewCard()
    {
        Api.Questions.Add(ToReview with { Status = QuestionStatus.Published, SubmittedAt = null, CurrentVersion = 1 });

        var page = RenderReview(ToReview.Id);

        page.WaitForAssertion(() => page.FindAll("#review-gone").Should().ContainSingle());
        page.Find("#review-gone").TextContent.Should().Contain("This question is no longer in review");
        page.Find("#review-gone a.app-link").GetAttribute("href").Should().Be(QueueRoute);
        page.FindAll("#review-approve, #review-reject").Should().BeEmpty();
    }

    [Fact]
    public void LoadError_ShowsTheErrorStateWithTryAgainAndTheBackLink_AndTryingAgainShowsTheQuestion()
    {
        Api.Questions.Add(ToReview);
        Api.ReviewFindFailure = (HttpStatusCode.InternalServerError, "common.unexpected_error");

        var page = RenderReview(ToReview.Id);

        page.WaitForAssertion(() => page.FindAll("#review-load-failed").Should().ContainSingle());
        page.Find("#review-load-failed").TextContent.Should().Contain("We could not load this list.").And.Contain("Try again");
        page.Find("#review-load-failed a.app-link").GetAttribute("href").Should().Be(QueueRoute);

        Api.ReviewFindFailure = null;
        page.Find(".app-retry").Click();

        page.WaitForAssertion(() => page.FindAll("#review-approve").Should().ContainSingle());
        page.FindAll("#review-load-failed").Should().BeEmpty();
    }
}
