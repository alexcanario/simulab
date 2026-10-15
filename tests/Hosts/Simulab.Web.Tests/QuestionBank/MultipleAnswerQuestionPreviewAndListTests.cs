using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-104 AC11 and AC13: the list names the third type, and the preview tells the student how many options to
/// mark, with the key and the explanation hidden until "show answer".
/// </summary>
public sealed class MultipleAnswerQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse MultiplePublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000060"),
        QuestionType.MultipleAnswer,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>Quais frases usam a crase corretamente?</p>",
        null,
        "<p>Crase antes de palavra feminina que aceita artigo.</p>",
        3,
        [
            new(0, "<p>Fui à escola.</p>", true),
            new(1, "<p>Fui a escola.</p>", false),
            new(2, "<p>Cheguei às cinco horas.</p>", true),
            new(3, "<p>Vou à pé.</p>", false)
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

    // AC13, BR6: the count comes from the key and is shown before the options.
    [Fact]
    public async Task Preview_MultipleAnswer_ShowsHowManyOptionsToMark_BeforeTheOptions()
    {
        var dialog = await OpenAsync(MultiplePublished);

        dialog.Find("#question-preview-select-count").TextContent.Trim().Should().Be("Mark 2 options.");
        var markup = dialog.Markup;
        markup.IndexOf("question-preview-select-count", StringComparison.Ordinal)
            .Should().BeGreaterThan(markup.IndexOf("question-preview-statement", StringComparison.Ordinal))
            .And.BeLessThan(markup.IndexOf("app-question-options", StringComparison.Ordinal));
        dialog.FindAll("ol.app-question-options li").Should().HaveCount(4);
        dialog.FindAll(".app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B", "C", "D");
    }

    // AC13: nothing reveals the key before the toggle, and the count carries no more than N.
    [Fact]
    public async Task Preview_MultipleAnswer_BeforeShowAnswer_HidesTheKeyAndTheExplanation()
    {
        var dialog = await OpenAsync(MultiplePublished);

        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Correct answer").And.NotContain("Crase antes de palavra feminina");
    }

    // AC13, BR6: after the reveal every correct option has the icon, the text and the border.
    [Fact]
    public async Task Preview_MultipleAnswer_ShowAnswer_MarksEveryCorrectOptionWithIconTextAndBorder()
    {
        var dialog = await OpenAsync(MultiplePublished);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("ol.app-question-options li.is-correct").Should().HaveCount(2));
        var correct = dialog.FindAll("ol.app-question-options li.is-correct");
        correct.Select(item => item.QuerySelector(".app-option-letter")!.TextContent).Should().Equal("A", "C");
        correct.Should().OnlyContain(item =>
            item.TextContent.Contains("Correct answer") && item.QuerySelector(".app-correct-label svg") != null);
        dialog.Find("#question-preview-explanation").TextContent.Should()
            .Contain("Crase antes de palavra feminina");
    }

    [Fact]
    public async Task Preview_MultipleAnswerDraftWithNoMarks_ShowsNoCountAndSaysThereIsNoKeyYet()
    {
        var unmarked = MultiplePublished with
        {
            Status = QuestionStatus.Draft,
            Options = [.. MultiplePublished.Options.Select(option => option with { IsCorrect = false })]
        };

        var dialog = await OpenAsync(unmarked);
        dialog.Find("#question-preview-toggle").Click();

        dialog.FindAll("#question-preview-select-count").Should().BeEmpty();
        dialog.WaitForAssertion(() => dialog.FindAll(".app-preview-no-key").Should().ContainSingle());
    }

    // The two older types do not get the line.
    [Fact]
    public async Task Preview_SingleChoice_HasNoSelectCountLine()
    {
        var dialog = await OpenAsync(Published);

        dialog.FindAll("#question-preview-select-count").Should().BeEmpty();
    }

    // AC11, BR10: the type chip of the list says it in text.
    [Fact]
    public void List_NamesTheTypeOfEachRow_InTheThirdLabel()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(MultiplePublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .Should().Equal("Single choice", "Multiple answer");
    }
}
