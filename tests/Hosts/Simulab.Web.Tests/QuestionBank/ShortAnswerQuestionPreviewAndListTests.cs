using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-108 AC13 and AC15: the list names the sixth type, and the preview draws the statement, an inert text box labelled
/// "Your answer" and the line "Type your answer."; the accepted answers and the explanation stay hidden until "show
/// answer", then they are listed with icon and text. A draft with no answer says "no key yet".
/// </summary>
public sealed class ShortAnswerQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse ShortPublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000195"),
        QuestionType.ShortAnswer,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Qual é a capital do Brasil? Digite [[1]] como está.</p>",
        null,
        "<p>Inaugurada por JK.</p>",
        3,
        [],
        CurrentVersion: 1,
        Blanks: [new(1, ["Brasília", "Brasilia"])]);

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

    // AC15, BR14: the statement as typed, an inert box named "Your answer", then the task line, below the statement.
    [Fact]
    public async Task Preview_ShortAnswer_ShowsTheStatement_AnInertBoxNamedYourAnswer_AndTheTaskLine()
    {
        var dialog = await OpenAsync(ShortPublished);

        dialog.Find("#question-preview-statement").TextContent.Should().Contain("Digite [[1]] como está.");
        dialog.FindAll("#question-preview-statement .app-blank").Should().BeEmpty("a short-answer statement has no marker");
        var box = dialog.FindComponents<AppTextField<string>>().Single(field => field.Instance.Id == "question-preview-short-answer").Instance;
        box.Label.Should().Be("Your answer");
        box.Disabled.Should().BeTrue();
        box.Value.Should().BeNullOrEmpty();
        dialog.Find("#question-preview-short-answer-instruction").TextContent.Trim().Should().Be("Type your answer.");
        var markup = dialog.Markup;
        markup.IndexOf("question-preview-short-answer-instruction", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    // AC15: nothing of the key shows before the toggle, and the box gives no hint of the answers' length.
    [Fact]
    public async Task Preview_ShortAnswer_BeforeShowAnswer_HidesTheAnswersAndTheExplanation()
    {
        var dialog = await OpenAsync(ShortPublished);

        dialog.Markup.Should().NotContain("Brasília").And.NotContain("Correct answer").And.NotContain("Inaugurada por JK.");
        dialog.FindAll("#question-preview-short-answer-key").Should().BeEmpty();
    }

    // AC15: after the reveal the accepted answers are listed with icon and text; the box stays empty.
    [Fact]
    public async Task Preview_ShortAnswer_ShowAnswer_ListsTheAcceptedAnswersWithIconAndText()
    {
        var dialog = await OpenAsync(ShortPublished);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-short-answer-key").Should().ContainSingle());
        dialog.Find("#question-preview-short-answer-key").TextContent.Trim().Should().Be("Accepted answers: Brasília, Brasilia");
        var label = dialog.Find("#question-preview-answer .app-correct-label");
        label.TextContent.Should().Contain("Correct answer");
        label.QuerySelector("svg").Should().NotBeNull("the key is never colour alone");
        dialog.FindComponents<AppTextField<string>>().Single(field => field.Instance.Id == "question-preview-short-answer")
            .Instance.Value.Should().BeNullOrEmpty();
        dialog.Find("#question-preview-explanation").TextContent.Should().Contain("Inaugurada por JK.");
    }

    // AC15: a draft with no answer says so.
    [Fact]
    public async Task Preview_ShortAnswerDraftWithNoAnswer_SaysItHasNoKeyYet()
    {
        var draft = ShortPublished with { Status = QuestionStatus.Draft, Blanks = [new(1, [])] };

        var dialog = await OpenAsync(draft);
        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.Find("#question-preview-short-answer-key").TextContent.Trim()
            .Should().Be("Accepted answers: no key yet"));
    }

    // AC13, BR12: the type chip of the list says it in text.
    [Fact]
    public void List_NamesTheShortAnswerType()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(ShortPublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .Should().Equal("Single choice", "Short answer");
    }
}
