using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-107 AC13 and AC15: the list names the fifth type, and the preview draws a numbered empty box for each marker with
/// the task line, the key and the explanation hidden until "show answer"; then each box shows its first accepted answer
/// and the full lists appear with icon and text. The version preview carries the blanks of the version.
/// </summary>
public sealed class FillInTheBlanksQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly DateTimeOffset Recorded = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly QuestionResponse FillInPublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000180"),
        QuestionType.FillInTheBlanks,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>A capital do Brasil é [[1]] e foi fundada em <strong>[[2]]</strong>.</p>",
        null,
        "<p>Inaugurada por JK.</p>",
        3,
        [],
        CurrentVersion: 1,
        Blanks:
        [
            new(1, ["Brasília", "Brasilia"]),
            new(2, ["1960"])
        ]);

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAsync(QuestionResponse question)
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, question },
            { dialog => dialog.UnsavedChanges, false }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true }));
        provider.WaitForAssertion(() => provider.FindAll("#question-preview-statement").Should().ContainSingle());

        return provider;
    }

    // AC15, BR15: one numbered empty box per marker, named "Blank n", then the task line.
    [Fact]
    public async Task Preview_FillIn_DrawsANumberedEmptyBoxForEachMarker_AndTheTaskLine()
    {
        var dialog = await OpenAsync(FillInPublished);

        var boxes = dialog.FindAll("#question-preview-statement .app-blank");
        boxes.Select(box => box.GetAttribute("aria-label")).Should().Equal("Blank 1", "Blank 2");
        boxes.Select(box => box.TextContent.Trim()).Should().Equal("1", "2");
        dialog.Find("#question-preview-statement").TextContent.Should().NotContain("[[");
        dialog.Find("#question-preview-statement strong .app-blank").Should().NotBeNull("formatting around a marker stays");
        dialog.Find("#question-preview-blanks-instruction").TextContent.Trim().Should().Be("Fill in each blank.");
        var markup = dialog.Markup;
        markup.IndexOf("question-preview-blanks-instruction", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    // AC15: nothing of the key shows before the toggle, and the boxes give no hint of the answers' length.
    [Fact]
    public async Task Preview_FillIn_BeforeShowAnswer_HidesTheKeyAndTheExplanation()
    {
        var dialog = await OpenAsync(FillInPublished);

        dialog.FindAll("#question-preview-statement .app-blank").Should().HaveCount(2);
        dialog.Markup.Should().NotContain("Brasília").And.NotContain("1960").And.NotContain("Correct answer")
            .And.NotContain("Inaugurada por JK.");
        dialog.FindAll("#question-preview-blanks-key").Should().BeEmpty();
    }

    // AC15: after the reveal each box shows its first answer and the full lists appear with icon and text.
    [Fact]
    public async Task Preview_FillIn_ShowAnswer_FillsTheBoxesAndListsEveryAcceptedAnswer()
    {
        var dialog = await OpenAsync(FillInPublished);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-blanks-key li").Should().HaveCount(2));
        dialog.FindAll("#question-preview-statement .app-blank").Select(box => box.TextContent.Trim())
            .Should().Equal("Brasília", "1960");
        dialog.FindAll("#question-preview-blanks-key li").Select(item => item.TextContent.Trim())
            .Should().Equal("Blank 1: Brasília, Brasilia", "Blank 2: 1960");
        var label = dialog.Find("#question-preview-answer .app-correct-label");
        label.TextContent.Should().Contain("Correct answer");
        label.QuerySelector("svg").Should().NotBeNull("the key is never colour alone");
        dialog.Find("#question-preview-explanation").TextContent.Should().Contain("Inaugurada por JK.");
    }

    // AC15: a draft blank with no answer says so.
    [Fact]
    public async Task Preview_FillInDraftWithAnUnansweredBlank_SaysItHasNoKeyYet()
    {
        var draft = FillInPublished with
        {
            Status = QuestionStatus.Draft,
            Blanks = [new(1, ["Brasília"]), new(2, [])]
        };

        var dialog = await OpenAsync(draft);
        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-blanks-key li").Should().HaveCount(2));
        dialog.FindAll("#question-preview-blanks-key li").Select(item => item.TextContent.Trim())
            .Should().Equal("Blank 1: Brasília", "Blank 2: no key yet");
        dialog.FindAll("#question-preview-statement .app-blank").Select(box => box.TextContent.Trim())
            .Should().Equal("Brasília", "2");
    }

    // AC15: the version preview shows the blanks the version recorded.
    [Fact]
    public void VersionPreview_FillIn_ShowsTheBlanksOfTheVersion()
    {
        var providers = RenderProviders();
        Api.Questions.Add(FillInPublished);
        Api.Versions[FillInPublished.Id] =
        [
            new QuestionVersionResponse(
                Guid.Parse("0198f5a0-0000-7000-8000-000000000991"),
                FillInPublished.Id,
                1,
                QuestionType.FillInTheBlanks,
                "pt-BR",
                "<p>Fundada em [[2]].</p>",
                null,
                null,
                [],
                null,
                false,
                null,
                Recorded,
                false,
                [],
                [new(2, ["MCMLX"])])
        ];
        var page = Render<QuestionForm>(parameters => parameters.Add(form => form.Id, FillInPublished.Id));
        page.WaitForElement("#question-version-actions-1-view");

        page.Find("#question-version-actions-1-view").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.FindAll("#question-preview-statement .app-blank").Should().ContainSingle());
        providers.Dialogs.Find("#question-preview-toggle").Click();
        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Find("#question-preview-blanks-key").TextContent.Should().Contain("Blank 2: MCMLX"));
    }

    // AC13, BR13: the type chip of the list says it in text.
    [Fact]
    public void List_NamesTheFillInTheBlanksType()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(FillInPublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .Should().Equal("Single choice", "Fill in the blanks");
    }
}
