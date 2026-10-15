using System.Globalization;
using Bunit;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-98 `/admin/questions/{id}` as far as bUnit reaches: the History card (AC15), the reason of an answer key
/// change on Save and on a refused Publish (AC16) and the absence of the card on a question that is not saved.
/// </summary>
public sealed class QuestionVersionFormTests : QuestionPageTestContext
{
    private const string HistoryCard = "section[aria-labelledby='question-section-history-title']";

    private static readonly DateTimeOffset Recorded = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static QuestionVersionResponse VersionOf(
        QuestionResponse question,
        int number,
        bool keyChanged = false,
        string? reason = null,
        string? statement = null,
        bool annulled = false) =>
        new(
            Guid.Parse($"0198f5a0-0000-7000-8000-0000000009{number:00}"),
            question.Id,
            number,
            question.Type,
            question.ContentLanguage,
            statement ?? question.StatementHtml,
            question.BaseText?.TextHtml,
            question.ExplanationHtml,
            question.Options,
            question.TrueFalseKey,
            keyChanged,
            reason,
            Recorded.AddDays(number),
            annulled);

    private IRenderedComponent<QuestionForm> RenderEdit(Guid id) =>
        Render<QuestionForm>(parameters => parameters.Add(form => form.Id, id));

    private void WaitForEditors(IRenderedComponent<QuestionForm> page, params string[] ids) =>
        page.WaitForAssertion(() => RichText.Editors.Keys.Should().Contain(ids));

    private static Task MarkCorrectAsync(IRenderedComponent<QuestionForm> page, int? position) =>
        page.InvokeAsync(() => page.FindComponent<MudRadioGroup<int?>>().Instance.ValueChanged.InvokeAsync(position));

    private static string[] Row(AngleSharp.Dom.IElement row) =>
        [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())];

    // AC15, UC5: the versions newest first, the current chip on the highest, the change label and the reason.
    [Fact]
    public void History_ListsTheVersionsNewestFirst_WithTheCurrentChipTheChangeLabelAndTheReason()
    {
        RenderProviders();
        Api.Questions.Add(Published with { CurrentVersion = 3 });
        Api.Versions[Published.Id] =
        [
            VersionOf(Published, 1),
            VersionOf(Published, 2, statement: "<p>Outro texto.</p>"),
            VersionOf(Published, 3, keyChanged: true, reason: "Recurso deferido pela banca.")
        ];

        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        var card = page.WaitForElement(HistoryCard);
        page.WaitForAssertion(() => card.QuerySelectorAll("tbody tr").Should().HaveCount(3));
        var rows = card.QuerySelectorAll("tbody tr").Select(Row).ToList();
        rows[0].Should().Contain(cell => cell.StartsWith('3') && cell.Contains("Current"));
        rows[0].Should().Contain("Answer key changed").And.Contain("Recurso deferido pela banca.");
        rows[1].Should().Contain("Content edited");
        rows[1].Should().NotContain(cell => cell.Contains("Current"));
        rows[2].Should().Contain("First publication");
        page.Find("#question-section-history-title").TextContent.Should().Contain("History");
    }

    // AC15: the key change is told by a warning chip, not by colour alone (the label is the text).
    [Fact]
    public void History_TheKeyChangeRowCarriesTheWarningToneAndItsOwnWords()
    {
        Api.Questions.Add(Published with { CurrentVersion = 2 });
        Api.Versions[Published.Id] = [VersionOf(Published, 1), VersionOf(Published, 2, keyChanged: true, reason: "Anulada.")];

        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        page.WaitForAssertion(() => page.FindAll($"{HistoryCard} .app-status-chip-warning").Should().ContainSingle());
        page.Find($"{HistoryCard} .app-status-chip-warning").TextContent.Should().Contain("Answer key changed");
    }

    // AC15: a question that was never published says when its first version is recorded.
    [Fact]
    public void History_ANeverPublishedQuestion_ShowsTheEmptyText()
    {
        Api.Questions.Add(Draft);

        var page = RenderEdit(Draft.Id);
        WaitForEditors(page, "question-statement");

        page.WaitForAssertion(() => page.Find(HistoryCard).TextContent.Should()
            .Contain("The first version is recorded when the question is published."));
    }

    // AC11, AC15: the card belongs to a saved question; a new one has none.
    [Fact]
    public void History_ANewQuestion_HasNoHistoryCard()
    {
        var page = Render<QuestionForm>();

        page.FindAll(HistoryCard).Should().BeEmpty();
    }

    // AC15, UC5: "View" opens the snapshot read only, with the key hidden until "show answer".
    [Fact]
    public void History_View_OpensTheRecordedVersionReadOnly_WithTheKeyHiddenUntilShowAnswer()
    {
        var providers = RenderProviders();
        Api.Questions.Add(Published with { CurrentVersion = 2 });
        Api.Versions[Published.Id] =
        [
            VersionOf(Published, 1, statement: "<p>Enunciado da primeira versão.</p>"),
            VersionOf(Published, 2)
        ];
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        page.WaitForElement("#question-version-actions-1-view");

        page.Find("#question-version-actions-1-view").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.FindAll("#question-preview-statement").Should().ContainSingle());
        var dialog = providers.Dialogs;
        dialog.Find("#question-preview-statement").InnerHtml.Should().Be("<p>Enunciado da primeira versão.</p>");
        dialog.Markup.Should().Contain("Version 1").And.Contain("Recorded on");
        dialog.Markup.Should().Contain(Recorded.AddDays(1).ToString("g", CultureInfo.CurrentCulture));
        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Correct answer");

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("li.is-correct").Should().ContainSingle());
        Api.Received.Should().Contain(call =>
            call.Path == $"/api/v1/question-bank/questions/{Published.Id}/versions/1");
    }

    // A version that cannot be read says so inside the History card, not in the Publication card.
    [Fact]
    public void History_ViewOfAVersionThatIsGone_ShowsTheRefusalInsideTheHistoryCard()
    {
        RenderProviders();
        Api.Questions.Add(Published with { CurrentVersion = 1 });
        Api.Versions[Published.Id] = [VersionOf(Published, 1)];
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        page.WaitForElement("#question-version-actions-1-view");
        Api.Versions.Clear();

        page.Find("#question-version-actions-1-view").Click();

        page.WaitForAssertion(() => page.Find(HistoryCard).TextContent.Should().Contain("This version does not exist."));
        page.Find("section[aria-labelledby='question-section-publication-title']").TextContent.Should()
            .NotContain("This version does not exist.");
    }

    // AC16, BR5: a published question whose key is not touched has no reason field.
    [Fact]
    public void Reason_OnAPublishedQuestionWithTheKeyUntouched_IsNotShown()
    {
        Api.Questions.Add(Published);

        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        page.FindAll("#question-change-reason").Should().BeEmpty();
    }

    // AC16: changing the correct option of a published question shows the field; Save is refused without it
    // and nothing is sent.
    [Fact]
    public async Task Reason_WhenTheKeyOfAPublishedQuestionChanges_ShowsTheFieldAndRefusesSaveWithoutIt()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");

        await MarkCorrectAsync(page, 1);

        page.WaitForAssertion(() => page.FindAll("#question-change-reason").Should().ContainSingle());
        page.Find("label[for='question-change-reason']").TextContent.Should().Contain("Reason for the answer key change");
        var writesBefore = Api.Received.Count(call => call.Method == HttpMethod.Put);

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-change-reason-description .app-field-error-text").TextContent
            .Should().Contain("Write the reason for the answer key change."));
        Api.Received.Count(call => call.Method == HttpMethod.Put).Should().Be(writesBefore);
    }

    [Fact]
    public async Task Reason_Typed_IsSentWithSave_AndTheFieldGoesAwayOnceSaved()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        await MarkCorrectAsync(page, 1);
        page.WaitForElement("#question-change-reason");

        await page.InvokeAsync(() => page.FindComponents<Simulab.Web.Components.Ui.AppTextField<string>>()
            .Single(field => field.Instance.Id == "question-change-reason")
            .Instance.ValueChanged.InvokeAsync("  Recurso deferido.  "));
        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        Api.LastSaved(HttpMethod.Put).ChangeReason.Should().Be("Recurso deferido.");
        page.WaitForAssertion(() => page.FindAll("#question-change-reason").Should().BeEmpty());
    }

    // AC16, BR5: a reason of more than 500 characters is refused before the call, with the Api's own words.
    [Fact]
    public async Task Reason_OverTheLimit_IsRefusedBeforeTheCall()
    {
        RenderProviders();
        Api.Questions.Add(Published);
        var page = RenderEdit(Published.Id);
        WaitForEditors(page, "question-statement");
        await MarkCorrectAsync(page, 2);
        page.WaitForElement("#question-change-reason");
        await page.InvokeAsync(() => page.FindComponents<Simulab.Web.Components.Ui.AppTextField<string>>()
            .Single(field => field.Instance.Id == "question-change-reason")
            .Instance.ValueChanged.InvokeAsync(new string('a', QuestionLimits.ChangeReasonMaxLength + 1)));

        page.Find(".app-form-save").Click();

        page.WaitForAssertion(() => page.Find("#question-change-reason-description .app-field-error-text").TextContent
            .Should().Contain("500 characters"));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Put);
    }

    // AC16, BR5: a draft whose publish the Api refuses for the missing reason: the card shows the field, the
    // focus goes to it, and the second click sends the reason and publishes.
    [Fact]
    public async Task Publish_RefusedForTheMissingReason_ShowsTheFieldInTheCard_AndTheSecondClickPublishes()
    {
        RenderProviders();
        var draft = Draft with { Options = [new(0, "<p>Fui à escola.</p>", false), new(1, "<p>Fui a escola.</p>", true)] };
        Api.Questions.Add(draft);
        Api.PublishNeedsReason = true;
        var page = RenderEdit(draft.Id);
        WaitForEditors(page, "question-statement");
        page.FindAll("#question-publish-reason").Should().BeEmpty();

        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => page.FindAll("#question-publish-reason").Should().ContainSingle());
        page.Find("section[aria-labelledby='question-section-publication-title']").TextContent.Should()
            .Contain("Write the reason for the answer key change.");
        Api.Received.Last(call => call.Path.EndsWith("/publish", StringComparison.Ordinal)).Body.Should().BeNull();

        await page.InvokeAsync(() => page.FindComponents<Simulab.Web.Components.Ui.AppTextField<string>>()
            .Single(field => field.Instance.Id == "question-publish-reason")
            .Instance.ValueChanged.InvokeAsync("Gabarito oficial retificado."));
        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => page.Find(".app-publication-row").TextContent.Should().Contain("Published"));
        var body = Api.Received.Last(call => call.Path.EndsWith("/publish", StringComparison.Ordinal)).Body;
        body.Should().Contain("Gabarito oficial retificado.");
        page.FindAll("#question-publish-reason").Should().BeEmpty();
    }

    // AC16: a publish from the list row refused for the missing reason shows the code's text in the alert;
    // the curator completes it on the question's page.
    [Fact]
    public void ListPublish_RefusedForTheMissingReason_ShowsTheCodesTextInTheTopAlert()
    {
        RenderProviders();
        Api.Questions.Add(Draft);
        Api.PublishNeedsReason = true;
        var page = Render<Questions>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));

        page.Find($"#question-actions-{Draft.Id}-publish").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should()
            .Contain("Write the reason for the answer key change."));
    }

    // AC15: publishing records version 1, so the card reloads and lists it.
    [Fact]
    public void History_AfterAPublish_ReloadsTheList()
    {
        RenderProviders();
        var complete = Draft with { Options = [new(0, "<p>Fui à escola.</p>", true), new(1, "<p>Fui a escola.</p>", false)] };
        Api.Questions.Add(complete);
        var page = RenderEdit(complete.Id);
        WaitForEditors(page, "question-statement");
        page.WaitForAssertion(() => page.Find(HistoryCard).TextContent.Should().Contain("The first version"));
        Api.Versions[complete.Id] = [VersionOf(complete with { Status = QuestionStatus.Published }, 1)];

        page.Find("#question-publish").Click();

        page.WaitForAssertion(() => page.Find(HistoryCard).TextContent.Should().Contain("First publication"));
    }
}
