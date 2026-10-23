using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-126 (part 1 of the question form in the new composition): the six sections are numbered whatever the type, and
/// the aside holds the state of the question and every publication action, with Publish as the screen's single amber
/// action (AC1, AC3 to AC8, AC11 to AC16). The rules and the Api answers are those of F-95 to F-107 and keep their tests.
/// </summary>
public sealed class QuestionFormCompositionTests : QuestionPageTestContext
{
    private const string Amber = ".app-action-key";
    private const string Block = "aside.app-form-aside section#question-section-publication";

    private static readonly DateTimeOffset Recorded = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static readonly QuestionResponse CompleteDraft = Draft with
    {
        Options = [new(0, "<p>Fui à escola.</p>", true), new(1, "<p>Fui a escola.</p>", false)]
    };

    private static readonly QuestionResponse InReview = CompleteDraft with
    {
        Status = QuestionStatus.InReview,
        SubmittedAt = Recorded
    };

    private static readonly QuestionResponse Annulled = Published with
    {
        Status = QuestionStatus.Annulled,
        CurrentVersion = 2,
        Annulment = new QuestionAnnulmentResponse("A banca anulou o item 12.", Recorded)
    };

    private static readonly QuestionResponse TrueFalse = Draft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000a30"),
        Type = QuestionType.TrueFalse,
        StatementHtml = "<p>A crase é obrigatória antes de verbos.</p>",
        Options = [],
        TrueFalseKey = false
    };

    private static readonly QuestionResponse Multiple = Draft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000a50"),
        Type = QuestionType.MultipleAnswer,
        Options =
        [
            new(0, "<p>Fui à escola.</p>", true),
            new(1, "<p>Fui a escola.</p>", false),
            new(2, "<p>Cheguei às cinco horas.</p>", true)
        ]
    };

    private static readonly QuestionResponse Matching = Draft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000a70"),
        Type = QuestionType.Matching,
        Options = [new(0, "<p>Lisboa</p>", false), new(1, "<p>Paris</p>", false), new(2, "<p>Roma</p>", false)],
        MatchItems = [new(0, "<p>França</p>", 1), new(1, "<p>Portugal</p>", 0)]
    };

    private static readonly QuestionResponse Blanks = Draft with
    {
        Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000b70"),
        Type = QuestionType.FillInTheBlanks,
        StatementHtml = "<p>A capital do Brasil é [[1]] e foi fundada em [[2]].</p>",
        Options = [],
        Blanks = [new(1, ["Brasília", "Brasilia"]), new(2, ["1960"])]
    };

    private IRenderedComponent<QuestionForm> RenderEdit(QuestionResponse question)
    {
        Api.Questions.Add(question);
        var page = Render<QuestionForm>(parameters => parameters.Add(form => form.Id, question.Id));
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));
        return page;
    }

    // -- AC1: six numbered sections, whatever the type ---------------------------------------------------------------

    [Theory]
    [InlineData("single choice", "question-section-options")]
    [InlineData("true false", "question-section-true-false-key")]
    [InlineData("multiple answer", "question-section-options")]
    [InlineData("matching", "question-section-matching")]
    [InlineData("fill in the blanks", "question-section-blanks")]
    public void Sections_AreNumberedOneToSixWithTheirHeadingsAndIdsUnchanged(string type, string keySection)
    {
        var question = type switch
        {
            "true false" => TrueFalse,
            "multiple answer" => Multiple,
            "matching" => Matching,
            "fill in the blanks" => Blanks,
            _ => Draft
        };

        var page = RenderEdit(question);

        var sections = page.FindAll(".app-form-layout .app-form-card section.app-section-card");
        sections.Should().HaveCount(6);
        sections.Select(section => section.QuerySelector(".app-section-card-number")?.TextContent.Trim())
            .Should().Equal("1", "2", "3", "4", "5", "6");
        sections.Select(section => section.QuerySelector(".app-section-card-number")!.GetAttribute("aria-hidden"))
            .Should().OnlyContain(hidden => hidden == "true");
        sections.Select(section => section.QuerySelector("h2")!.Id).Should().Equal(
            "question-section-classification-title",
            "question-section-origin-title",
            "question-section-statement-title",
            "question-section-base-text-title",
            $"{keySection}-title",
            "question-section-explanation-title");
        sections.Select(section => section.QuerySelector("h2")!.TextContent.Trim()).Should().NotContain(text => char.IsDigit(text[0]),
            "the number is not part of the heading text");
    }

    // -- AC3, AC4, AC5: the publication block lives in the aside ---------------------------------------------------------

    [Fact]
    public void SavedDraft_TheAsideHoldsTheChipPreviewSendForReviewAndPublishAndNoCardBelow()
    {
        var page = RenderEdit(CompleteDraft);

        page.FindAll("#question-section-publication").Should().ContainSingle();
        page.FindAll(".app-form-card #question-section-publication").Should().BeEmpty("the Publication card below the form no longer exists");
        var block = page.Find(Block);
        block.QuerySelector(".app-status-chip")!.TextContent.Should().Contain("Draft");
        block.QuerySelector("button#question-preview").Should().NotBeNull();
        block.QuerySelector("button#question-send-for-review").Should().NotBeNull();
        block.QuerySelector("button#question-publish").Should().NotBeNull();
        block.QuerySelectorAll("button").Select(button => button.Id).Should().Equal(
            ["question-preview", "question-send-for-review", "question-publish"], "the order of the old card is kept");
        page.Find("aside.app-form-aside").QuerySelectorAll(".app-form-aside-row-label").Select(label => label.TextContent.Trim())
            .Should().NotContain("Status", "the chip of the block is the one place for the status");
    }

    [Fact]
    public void Published_TheAsideHoldsPreviewUnpublishAnnulAndNothingAmber()
    {
        var page = RenderEdit(Published);

        page.Find(Block).QuerySelectorAll("button").Select(button => button.Id).Should().Equal(
            "question-preview", "question-unpublish", "question-annul");
        page.FindAll(Amber).Should().BeEmpty();
        page.Find("#question-annul").ClassList.Should().Contain("mud-button-outlined-error");
    }

    [Fact]
    public void InReview_TheAsideHoldsPreviewAndWithdrawAndNothingAmber()
    {
        var page = RenderEdit(InReview);

        page.Find(Block).QuerySelectorAll("button").Select(button => button.Id).Should().Equal("question-preview", "question-withdraw");
        page.FindAll(Amber).Should().BeEmpty();
    }

    [Fact]
    public void Annulled_TheAsideHoldsTheAnnulmentDateAndReasonPreviewAndReinstateAndNothingAmber()
    {
        var page = RenderEdit(Annulled);

        var block = page.Find(Block);
        block.QuerySelector("#question-annulment-date").Should().NotBeNull();
        block.QuerySelector("#question-annulment-reason")!.TextContent.Should().Contain("A banca anulou o item 12.");
        block.QuerySelectorAll("button").Select(button => button.Id).Should().Equal("question-preview", "question-reinstate");
        page.FindAll(Amber).Should().BeEmpty();
    }

    [Fact]
    public void NewQuestion_TheAsideSaysToSaveFirstAndHasNoPublicationButton()
    {
        var page = Render<QuestionForm>();

        var block = page.Find(Block);
        block.TextContent.Should().Contain("Save the question first to preview or publish it.");
        block.QuerySelectorAll("button").Should().BeEmpty();
        page.FindAll(Amber).Should().BeEmpty();
    }

    // -- AC6, AC7, AC8: Publish, the single amber action --------------------------------------------------------------

    [Fact]
    public void CompleteSavedDraft_PublishIsTheOneEnabledCompactAmberKeyAndPublishingEndsInTheBlockOfAPublishedQuestion()
    {
        var providers = RenderProviders();
        var page = RenderEdit(CompleteDraft);

        page.FindAll(Amber).Should().ContainSingle();
        page.Find("#question-publish").ClassList.Should().Contain("app-action-key");
        page.Find("#question-publish").HasAttribute("disabled").Should().BeFalse();
        page.Find(".app-action").ClassList.Should().Contain("app-action-compact");

        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Draft.Id}/publish"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question published."));
        page.WaitForAssertion(() => page.Find(Block).QuerySelector("button#question-unpublish").Should().NotBeNull());
        page.FindAll(Amber).Should().BeEmpty("no amber action remains once it is published");
        page.Find(Block).QuerySelector(".app-status-chip")!.TextContent.Should().Contain("Published");
    }

    [Fact]
    public void DraftWithAnIncompleteChecklist_PublishIsADisabledKeyWithTheVisibleReasonItPointsAt()
    {
        var page = RenderEdit(Draft);

        var publish = page.Find("button#question-publish");
        publish.HasAttribute("disabled").Should().BeTrue();
        publish.GetAttribute("aria-describedby").Should().Be("question-publication-reason");
        page.Find(".app-action").ClassList.Should().Contain("app-action-disabled");
        page.Find($"{Block} #question-publication-reason").TextContent.Trim().Should().Be("Complete the checklist before publishing.");
        page.Find("#question-send-for-review").HasAttribute("disabled").Should().BeTrue();
        page.Find("#question-send-for-review").GetAttribute("aria-describedby").Should().Be("question-publication-reason");
    }

    [Fact]
    public async Task DraftWithUnsavedChanges_PublishAndSendForReviewAreDisabledWithTheSaveFirstReason()
    {
        var page = RenderEdit(CompleteDraft);

        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Mudou</p>"));

        page.WaitForAssertion(() => page.Find($"{Block} #question-publication-reason").TextContent.Trim().Should().Be("Save your changes first."));
        page.Find("button#question-publish").HasAttribute("disabled").Should().BeTrue();
        page.Find("button#question-send-for-review").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void PublishRunning_TheAmberKeyIsDisabledAndShowsProgress()
    {
        RenderProviders();
        var gate = new TaskCompletionSource();
        var page = RenderEdit(CompleteDraft);
        Api.WriteGate = gate;
        try
        {
            page.Find("#question-publish").Click();

            page.WaitForAssertion(() =>
            {
                page.Find("button#question-publish").ClassList.Should().Contain("app-action-key");
                page.Find("button#question-publish").HasAttribute("disabled").Should().BeTrue();
                page.Find("button#question-publish").QuerySelector(".mud-progress-circular").Should().NotBeNull();
                page.Find("#question-preview").HasAttribute("disabled").Should().BeTrue();
                page.Find("#question-send-for-review").HasAttribute("disabled").Should().BeTrue();
            });
        }
        finally
        {
            gate.TrySetResult();
        }
    }

    // -- AC11, AC12, AC13: Save stays the one navy primary, and the frozen and transcription states -------------------------

    [Fact]
    public async Task SaveOfADraftWithChanges_SavesAndDoesNotPublishAndIsTheOnlyNavyPrimaryButton()
    {
        RenderProviders();
        var page = RenderEdit(CompleteDraft);
        page.FindAll("button.mud-button-filled-primary").Should().ContainSingle();
        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Mudou</p>"));

        page.Find("button.app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/publish", StringComparison.Ordinal), "Save never publishes");
        page.FindAll("button.mud-button-filled-primary").Should().ContainSingle();
    }

    [Theory]
    [InlineData("in review", "question-withdraw")]
    [InlineData("annulled", "question-reinstate")]
    public void FrozenQuestion_HasNoSaveAndTheWayOutIsInTheAside(string state, string wayOut)
    {
        var page = RenderEdit(state == "in review" ? InReview : Annulled);

        page.FindAll("button.app-form-save").Should().BeEmpty();
        page.FindAll("button.mud-button-filled-primary").Should().BeEmpty();
        page.Find(Block).QuerySelector($"button#{wayOut}").Should().NotBeNull();
    }

    [Fact]
    public void Transcription_HasNoPublicationBlockNoAmberActionAndSaveAndContinueIsThePrimary()
    {
        Api.Editions.Add(GuardEdition);
        Api.NoticeSubjects[GuardEdition.Id] = [Basic, Specific];
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/admin/questions/transcribe?examEditionId={GuardEdition.Id}");

        var page = Render<QuestionForm>();

        page.FindAll("#question-section-publication").Should().BeEmpty();
        page.FindAll(Amber).Should().BeEmpty();
        page.Find("button.mud-button-filled-primary").TextContent.Trim().Should().Be("Save and continue");
        page.FindAll("button.mud-button-filled-primary").Should().ContainSingle();
    }

    // -- AC14, AC15: DOM order and the cards below --------------------------------------------------------------------------

    [Fact]
    public void DomOrder_IsTheFormCardThenTheAsideThenTheHistoryCards()
    {
        var page = RenderEdit(Published);

        var markup = page.Markup;
        var form = markup.IndexOf("app-form-card", StringComparison.Ordinal);
        var asideStart = markup.IndexOf("<aside", StringComparison.Ordinal);
        var asideEnd = markup.IndexOf("</aside>", StringComparison.Ordinal);
        var history = markup.IndexOf("question-section-history-title", StringComparison.Ordinal);
        form.Should().BeGreaterThanOrEqualTo(0);
        asideStart.Should().BeGreaterThan(form);
        history.Should().BeGreaterThan(asideEnd);
    }

    [Fact]
    public void HistoryCards_StayBelowTheAsideAndAreNotNumbered()
    {
        var page = RenderEdit(Published);

        var history = page.Find("section[aria-labelledby='question-section-history-title']");
        history.QuerySelector(".app-section-card-number").Should().BeNull();
        history.QuerySelector(".app-section-card-icon").Should().NotBeNull();
        page.Find("aside.app-form-aside").QuerySelector("#question-section-history").Should().BeNull();
        page.FindAll(".app-form-aside [id='question-section-history-title']").Should().BeEmpty();
    }

    // -- AC16: the status-reason dialog keeps its composition ----------------------------------------------------------------

    [Fact]
    public void AnnulDialog_HasItsReasonFieldAndNoAmberAction()
    {
        var providers = RenderProviders();
        var page = RenderEdit(Published);

        page.Find("#question-annul").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll("#question-status-reason").Should().NotBeEmpty());
        providers.Dialogs.FindAll(Amber).Should().BeEmpty();
    }
}
