using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-109 AC13 and AC16: the list names the seventh and eighth type, and the preview draws the statement, an inert
/// multi-line box labelled "Your answer" or "Your essay", the task line and the limit line; the rubric and the model
/// answer stay hidden until "show answer", then the rubric is listed with its total and the model answer follows. A
/// draft with no criterion says "no rubric yet".
/// </summary>
public sealed class RubricQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse EssayPublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000220"),
        QuestionType.Essay,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Escreva sobre a cidade. Use [[1]] como está.</p>",
        null,
        "<p>Um bom texto defende uma tese.</p>",
        3,
        [],
        CurrentVersion: 1,
        Rubric: [new(1, "Theme", "Meets the prompt", 30), new(2, "Argument", "Defends a thesis", 40), new(3, "Language", null, 30)],
        AnswerSizeMin: 7,
        AnswerSizeMax: 30);

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

    private static AppTextField<string> Box(IRenderedComponent<MudDialogProvider> dialog) =>
        dialog.FindComponents<AppTextField<string>>().Single(field => field.Instance.Id == "question-preview-written-answer").Instance;

    // AC16, BR14: the statement as typed, an inert multi-line box named "Your essay", the task line and the limit line.
    [Fact]
    public async Task Preview_Essay_ShowsTheStatement_AnInertBoxNamedYourEssay_TheTaskAndTheLimitLine()
    {
        var dialog = await OpenAsync(EssayPublished);

        dialog.Find("#question-preview-statement").TextContent.Should().Contain("Use [[1]] como está.");
        dialog.FindAll("#question-preview-statement .app-blank").Should().BeEmpty("the statement has no marker");
        var box = Box(dialog);
        box.Label.Should().Be("Your essay");
        box.Disabled.Should().BeTrue();
        box.Lines.Should().BeGreaterThan(1);
        box.Value.Should().BeNullOrEmpty();
        dialog.Find("#question-preview-written-instruction").TextContent.Trim().Should().Be("Write your essay.");
        dialog.Find("#question-preview-written-limit").TextContent.Trim().Should().Be("Between 7 and 30 lines.");
        var markup = dialog.Markup;
        markup.IndexOf("question-preview-written-instruction", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, 30, "Up to 30 lines.")]
    [InlineData(7, null, "At least 7 lines.")]
    public async Task Preview_Essay_WithOnlyOneEdge_SaysIt(int? min, int? max, string line)
    {
        var dialog = await OpenAsync(EssayPublished with { AnswerSizeMin = min, AnswerSizeMax = max });

        dialog.Find("#question-preview-written-limit").TextContent.Trim().Should().Be(line);
    }

    [Fact]
    public async Task Preview_Essay_WithNoLimit_HasNoLimitLine()
    {
        var dialog = await OpenAsync(EssayPublished with { AnswerSizeMin = null, AnswerSizeMax = null });

        dialog.Find("#question-preview-written-instruction").TextContent.Trim().Should().Be("Write your essay.");
        dialog.FindAll("#question-preview-written-limit").Should().BeEmpty();
    }

    [Fact]
    public async Task Preview_OpenAnswer_ShowsTheBoxNamedYourAnswer_TheTaskAndTheCharacterLimit()
    {
        var dialog = await OpenAsync(EssayPublished with { Type = QuestionType.OpenAnswer, AnswerSizeMin = null, AnswerSizeMax = 500 });

        Box(dialog).Label.Should().Be("Your answer");
        dialog.Find("#question-preview-written-instruction").TextContent.Trim().Should().Be("Write your answer.");
        dialog.Find("#question-preview-written-limit").TextContent.Trim().Should().Be("Up to 500 characters.");
    }

    // AC16: nothing of the rubric or the model answer shows before the toggle.
    [Fact]
    public async Task Preview_Essay_BeforeShowAnswer_HidesTheRubricAndTheModelAnswer()
    {
        var dialog = await OpenAsync(EssayPublished);

        Box(dialog).Label.Should().Be("Your essay", "the writing box is there before the reveal");
        dialog.Markup.Should().NotContain("Meets the prompt").And.NotContain("Grading rubric").And.NotContain("Um bom texto defende uma tese.");
        dialog.FindAll("#question-preview-rubric").Should().BeEmpty();
    }

    // AC16: after the reveal the rubric is a table with its total, with icon and text; then the model answer.
    [Fact]
    public async Task Preview_Essay_ShowAnswer_ListsTheRubricWithItsTotal_ThenTheModelAnswer()
    {
        var dialog = await OpenAsync(EssayPublished);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-rubric").Should().ContainSingle());
        var label = dialog.Find("#question-preview-answer .app-correct-label");
        label.TextContent.Should().Contain("Grading rubric");
        label.QuerySelector("svg").Should().NotBeNull("the key is never colour alone");
        var rows = dialog.FindAll("#question-preview-rubric tbody tr").Select(row => row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()).ToArray()).ToList();
        rows.Should().HaveCount(4);
        rows[0].Should().Equal("Theme", "Meets the prompt", "30");
        rows[1].Should().Equal("Argument", "Defends a thesis", "40");
        rows[2][0].Should().Be("Language");
        rows[2][2].Should().Be("30");
        rows[3][0].Should().Be("Total");
        rows[3][2].Should().Be("100");
        dialog.Find("#question-preview-explanation").TextContent.Should().Contain("Um bom texto defende uma tese.");
        Box(dialog).Value.Should().BeNullOrEmpty();
    }

    // AC16: a draft with no criterion says so.
    [Fact]
    public async Task Preview_EssayDraftWithNoCriterion_SaysNoRubricYet()
    {
        var dialog = await OpenAsync(EssayPublished with { Status = QuestionStatus.Draft, Rubric = [] });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.Markup.Should().Contain("no rubric yet"));
        dialog.FindAll("#question-preview-rubric tbody tr").Should().BeEmpty();
    }

    // AC13, BR13: the type chip of the list says it in text.
    [Fact]
    public void List_NamesTheOpenAnswerAndEssayTypes()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(EssayPublished);
        Api.Questions.Add(EssayPublished with { Id = Guid.Parse("0198f5a0-0000-7000-8000-000000000221"), Type = QuestionType.OpenAnswer });

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .Should().Equal("Single choice", "Essay", "Open answer");
    }
}
