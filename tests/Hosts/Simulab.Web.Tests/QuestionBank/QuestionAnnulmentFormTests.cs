using Bunit;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-101 `/admin/questions/{id}` as far as bUnit reaches: the Annul button and its dialog (AC13), the frozen
/// form of an annulled question and the Reinstate button (AC13, AC14) and the History card rows (AC15). The
/// rules are the Api's and have their own tests.
/// </summary>
public sealed class QuestionAnnulmentFormTests : QuestionPageTestContext
{
    private const string HistoryCard = "section[aria-labelledby='question-section-history-title']";
    private const string PublicationCard = "section[aria-labelledby='question-section-publication-title']";
    private const string ReasonId = "question-status-reason";

    private static readonly DateTimeOffset Recorded = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static readonly QuestionResponse AnnulledQuestion = Published with
    {
        Status = QuestionStatus.Annulled,
        CurrentVersion = 2,
        Annulment = new QuestionAnnulmentResponse("A banca anulou o item 12.", Recorded)
    };

    private static QuestionVersionResponse VersionOf(
        QuestionResponse question,
        int number,
        string? reason = null,
        bool annulled = false) =>
        new(
            Guid.Parse($"0198f5a0-0000-7000-8000-0000000008{number:00}"),
            question.Id,
            number,
            question.Type,
            question.ContentLanguage,
            question.StatementHtml,
            question.BaseText?.TextHtml,
            question.ExplanationHtml,
            question.Options,
            question.TrueFalseKey,
            false,
            reason,
            Recorded.AddDays(number),
            annulled);

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain("question-statement"));

    private static Task TypeReasonAsync(IRenderedComponent<MudDialogProvider> dialogs, string reason) =>
        dialogs.InvokeAsync(() => dialogs.FindComponents<AppTextField<string>>()
            .Single(field => field.Instance.Id == ReasonId)
            .Instance.ValueChanged.InvokeAsync(reason));

    private static string[] Row(AngleSharp.Dom.IElement row) =>
        [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())];

    // UC1: only a published question offers Annul; a draft and an annulled one do not.
    [Fact]
    public void Publication_OnlyAPublishedQuestion_OffersAnnul()
    {
        Api.Questions.Add(Published);
        Api.Questions.Add(Draft);

        var published = RenderEdit(Published.Id);
        WaitForEditors(published);
        published.WaitForAssertion(() => published.FindAll("#question-annul").Should().ContainSingle());
        published.Find("#question-annul").TextContent.Should().Contain("Annul question");
        published.FindAll("#question-reinstate").Should().BeEmpty();

        var draft = RenderEdit(Draft.Id);
        draft.WaitForAssertion(() => draft.FindAll("#question-publish").Should().ContainSingle());
        draft.FindAll("#question-annul").Should().BeEmpty();
    }

    // AC13, BR3: the dialog asks for the reason; an empty one is refused before the call, with the Api's words.
    [Fact]
    public void Annul_WithAnEmptyReason_IsRefusedInTheDialog_AndNothingIsSent()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-annul");

        page.Find("#question-annul").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().ContainSingle());
        providers.Dialogs.Markup.Should().Contain("Annul this question?");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Write the reason."));
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/annul", StringComparison.Ordinal));
    }

    [Fact]
    public void Annul_CancelledInTheDialog_SendsNothingAndLeavesTheQuestionPublished()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-annul");
        page.Find("#question-annul").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        providers.Dialogs.Find("#question-status-reason-cancel").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().BeEmpty());
        Api.Received.Should().NotContain(call => call.Path.EndsWith("/annul", StringComparison.Ordinal));
        page.FindAll("#question-annul").Should().ContainSingle();
    }

    // AC13, UC1: the reason goes trimmed, and the card, the alert and the fields tell the question is frozen.
    [Fact]
    public async Task Annul_WithAReason_SendsItTrimmed_AndTheFormBecomesFrozen()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-annul");
        page.Find("#question-annul").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        await TypeReasonAsync(providers.Dialogs, "  A banca anulou o item 12.  ");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{Published.Id}/annul"));
        FakeQuestionBankApi.Read<AnnulQuestionRequest>(Api.Received.Last(call => call.Path.EndsWith("/annul", StringComparison.Ordinal)).Body)
            .Reason.Should().Be("A banca anulou o item 12.");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question annulled."));

        page.WaitForAssertion(() => page.FindAll("#question-annulled-alert").Should().ContainSingle());
        page.Find("#question-annulled-alert").TextContent.Should().Contain("This question is annulled and cannot be edited.");
        var card = page.Find(PublicationCard);
        card.TextContent.Should().Contain("Annulled");
        page.Find("#question-annulment-reason").TextContent.Should().Contain("A banca anulou o item 12.");
        page.Find("#question-annulment-date").TextContent.Should().Contain("Annulled on");
        page.FindAll("#question-reinstate").Should().ContainSingle();
        page.FindAll("#question-annul, #question-unpublish, #question-publish, .app-form-save").Should().BeEmpty();
        RichText.Editors["question-statement"].Disabled.Should().BeTrue();
        page.FindAll(".app-status-chip-error").Should().NotBeEmpty();
    }

    // The Api refuses the annulment (say, a race): the dialog stays and shows the code's text, nothing freezes.
    [Fact]
    public async Task Annul_RefusedByTheApi_ShowsTheCodesTextInTheDialog_AndTheFormStaysEditable()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published);
        Api.AnnulmentFailure = (System.Net.HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionVersionConflict);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-annul");
        page.Find("#question-annul").Click();
        providers.Dialogs.WaitForElement($"#{ReasonId}");

        await TypeReasonAsync(providers.Dialogs, "Anulada.");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Reload the page and try again."));
        page.FindAll("#question-annulled-alert").Should().BeEmpty();
        RichText.Editors["question-statement"].Disabled.Should().BeFalse();
    }

    // Design pass: annulling acts on what is stored, so unsaved changes block it, with the reason in words.
    [Fact]
    public async Task Annul_WithUnsavedChanges_IsDisabledWithTheReasonInWords()
    {
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-annul");
        page.Find("#question-annul").HasAttribute("disabled").Should().BeFalse();

        await page.InvokeAsync(() => RichText.TypeAsync("question-statement", "<p>Mudou</p>"));

        page.WaitForAssertion(() => page.Find("#question-annul").HasAttribute("disabled").Should().BeTrue());
        page.Find(PublicationCard).TextContent.Should().Contain("Save your changes first.");
    }

    // AC13: a question that was already annulled opens frozen, with the card, the date and the reason.
    [Fact]
    public void AnAnnulledQuestion_OpensFrozen_WithTheReasonTheDateAndOnlyReinstateToLeave()
    {
        Api.Questions.Add(AnnulledQuestion);
        Api.Versions[AnnulledQuestion.Id] = [VersionOf(AnnulledQuestion, 1), VersionOf(AnnulledQuestion, 2, "A banca anulou o item 12.", annulled: true)];

        var page = RenderEdit(AnnulledQuestion.Id);
        WaitForEditors(page);

        page.WaitForAssertion(() => page.FindAll("#question-annulled-alert").Should().ContainSingle());
        page.Find("#question-annulment-reason").TextContent.Should().Contain("A banca anulou o item 12.");
        page.FindAll("#question-reinstate").Should().ContainSingle();
        page.FindAll("#question-annul, #question-unpublish, #question-publish, .app-form-save").Should().BeEmpty();
        page.FindAll("#question-preview").Should().ContainSingle("the preview is a read and stays");
        RichText.Editors.Values.Should().OnlyContain(editor => editor.Disabled);
    }

    // AC14, UC2: reinstating asks the reason, sends it, and the form is editable again.
    [Fact]
    public async Task Reinstate_WithAReason_SendsItTrimmed_AndTheFormIsEditableAgain()
    {
        var providers = RenderProviders();
        Api.Questions.Add(AnnulledQuestion);
        Api.Versions[AnnulledQuestion.Id] = [VersionOf(AnnulledQuestion, 1), VersionOf(AnnulledQuestion, 2, "A banca anulou o item 12.", annulled: true)];
        var page = RenderEdit(AnnulledQuestion.Id);
        WaitForEditors(page);
        page.WaitForElement("#question-reinstate");

        page.Find("#question-reinstate").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.FindAll($"#{ReasonId}").Should().ContainSingle());
        providers.Dialogs.Markup.Should().Contain("Reinstate this question?");
        await TypeReasonAsync(providers.Dialogs, "  A banca reverteu.  ");
        providers.Dialogs.Find("#question-status-reason-confirm").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == $"/api/v1/question-bank/questions/{AnnulledQuestion.Id}/reinstate"));
        FakeQuestionBankApi.Read<ReinstateQuestionRequest>(Api.Received.Last(call => call.Path.EndsWith("/reinstate", StringComparison.Ordinal)).Body)
            .Reason.Should().Be("A banca reverteu.");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Question reinstated."));
        page.WaitForAssertion(() => page.FindAll("#question-annulled-alert").Should().BeEmpty());
        page.FindAll("#question-annul").Should().ContainSingle();
        page.FindAll("#question-unpublish").Should().ContainSingle();
        page.FindAll(".app-form-save").Should().ContainSingle();
        RichText.Editors["question-statement"].Disabled.Should().BeFalse();
        page.Find(PublicationCard).TextContent.Should().Contain("Published");
    }

    // AC15, UC4: the annulment and the reinstatement rows have their own label and tone, and their reasons.
    [Fact]
    public void History_TheAnnulmentAndTheReinstatement_HaveTheirOwnLabelToneAndReason()
    {
        var question = Published with { CurrentVersion = 3 };
        Api.Questions.Add(question);
        Api.Versions[question.Id] =
        [
            VersionOf(question, 1),
            VersionOf(question, 2, "A banca anulou o item 12.", annulled: true),
            VersionOf(question, 3, "A banca reverteu.")
        ];

        var page = RenderEdit(question.Id);
        WaitForEditors(page);

        var card = page.WaitForElement(HistoryCard);
        page.WaitForAssertion(() => card.QuerySelectorAll("tbody tr").Should().HaveCount(3));
        var rows = card.QuerySelectorAll("tbody tr").Select(Row).ToList();
        rows[0].Should().Contain("Reinstated").And.Contain("A banca reverteu.");
        rows[1].Should().Contain("Annulled").And.Contain("A banca anulou o item 12.");
        rows[2].Should().Contain("First publication");
        page.FindAll($"{HistoryCard} .app-status-chip-error").Should().ContainSingle();
        page.Find($"{HistoryCard} .app-status-chip-error").TextContent.Should().Contain("Annulled");
    }
}
