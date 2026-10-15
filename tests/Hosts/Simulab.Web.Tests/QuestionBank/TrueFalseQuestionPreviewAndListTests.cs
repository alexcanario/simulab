using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-97 AC9 and AC11: the list tells the two types apart, and the preview shows a true/false question as a
/// student reads it, with the key and the explanation hidden until "show answer".
/// </summary>
public sealed class TrueFalseQuestionPreviewAndListTests : QuestionPageTestContext
{
    private static readonly QuestionResponse TrueFalsePublished = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000040"),
        QuestionType.TrueFalse,
        QuestionStatus.Published,
        Crase.Id,
        Crase.Name,
        Portuguese.Id,
        Portuguese.Name,
        "pt-BR",
        "<p>A crase é <strong>obrigatória</strong> antes de verbos.</p>",
        new QuestionBaseTextResponse(
            Guid.Parse("0198f5a0-0000-7000-8000-0000000000d8"),
            "Texto sobre crase",
            "<p>Texto-base sobre crase.</p>"),
        "<p>Nunca se usa crase antes de verbo.</p>",
        2,
        [],
        TrueFalseKey: false);

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

    // AC11, BR7: base text, statement and the two answers, as inert labelled text.
    [Fact]
    public async Task Preview_TrueFalse_ShowsBaseTextStatementAndTheTwoAnswersAsInertText()
    {
        var dialog = await OpenAsync(TrueFalsePublished);

        var markup = dialog.Markup;
        var baseText = markup.IndexOf("question-preview-base-text", StringComparison.Ordinal);
        var statement = markup.IndexOf("question-preview-statement", StringComparison.Ordinal);
        var answers = markup.IndexOf("question-preview-true-false", StringComparison.Ordinal);
        baseText.Should().BeGreaterThan(-1);
        statement.Should().BeGreaterThan(baseText);
        answers.Should().BeGreaterThan(statement);

        var list = dialog.Find("#question-preview-true-false");
        list.GetAttribute("aria-label").Should().Be("Answers");
        list.QuerySelectorAll("li").Select(item => item.TextContent.Trim()).Should().Equal("True", "False");
        list.QuerySelectorAll("input, button, [role='radio'], [tabindex]").Should().BeEmpty("the answers are inert");
        dialog.FindAll("ol.app-question-options").Should().BeEmpty("a true/false question has no lettered options");
        dialog.FindAll(".app-option-letter").Should().BeEmpty();
    }

    [Fact]
    public async Task Preview_TrueFalse_BeforeShowAnswer_HidesTheKeyAndTheExplanation()
    {
        var dialog = await OpenAsync(TrueFalsePublished);

        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Correct answer").And.NotContain("Nunca se usa crase");
        dialog.Find("#question-preview-toggle").GetAttribute("aria-expanded").Should().Be("false");
    }

    // AC11, BR7: after the reveal the key is marked with an icon, a text and a border, never colour alone.
    [Fact]
    public async Task Preview_TrueFalse_ShowAnswer_MarksTheKeyWithIconTextAndBorder_AndShowsTheExplanation()
    {
        var dialog = await OpenAsync(TrueFalsePublished with { TrueFalseKey = false });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-true-false li.is-correct").Should().ContainSingle());
        var correct = dialog.Find("#question-preview-true-false li.is-correct");
        correct.TextContent.Should().Contain("False").And.Contain("Correct answer");
        correct.QuerySelector(".app-correct-label svg").Should().NotBeNull("the key is never colour alone");
        dialog.Find("#question-preview-explanation").TextContent.Should().Contain("Nunca se usa crase antes de verbo.");
    }

    [Fact]
    public async Task Preview_TrueFalse_KeyTrue_MarksTheTrueAnswer()
    {
        var dialog = await OpenAsync(TrueFalsePublished with { TrueFalseKey = true });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-true-false li.is-correct").Should().ContainSingle());
        dialog.Find("#question-preview-true-false li.is-correct").TextContent.Should().Contain("True");
    }

    [Fact]
    public async Task Preview_TrueFalseDraftWithNoKey_ShowAnswer_SaysThereIsNoKeyYet()
    {
        var dialog = await OpenAsync(TrueFalsePublished with { Status = QuestionStatus.Draft, TrueFalseKey = null });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll(".app-preview-no-key").Should().ContainSingle());
        dialog.FindAll("#question-preview-true-false li.is-correct").Should().BeEmpty();
    }

    // AC9, BR8: each row names its type in a text chip, in the last column so the F-95 columns keep their places.
    [Fact]
    public void List_NamesTheTypeOfEachRowInATextChip()
    {
        Api.Questions.Add(Draft);
        Api.Questions.Add(TrueFalsePublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.FindAll("thead th").Select(header => header.TextContent.Trim()).Should().Contain("Type");
        var types = page.FindAll("tbody tr")
            .Select(row => row.QuerySelector("td .app-chip")!.TextContent.Trim())
            .ToList();
        types.Should().Equal("Single choice", "True/false");
    }

    // AC9: the type is text, so a row is never told apart by colour or position alone.
    [Fact]
    public void List_TheTypeChipIsPlainTextOfTheRow_WithNoStatusToneOfItsOwn()
    {
        Api.Questions.Add(TrueFalsePublished);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));
        var chip = page.Find("tbody tr td .app-chip");
        chip.TextContent.Trim().Should().Be("True/false");
        chip.ClassList.Should().NotContain(name => name.StartsWith("app-status-chip", StringComparison.Ordinal));
    }
}
