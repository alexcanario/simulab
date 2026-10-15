using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-95 AC12: the preview dialog renders the stored question the way a student reads it, and reveals the key and
/// the explanation only on request. Rendered with its real parameters, through the dialog service.
/// </summary>
public sealed class QuestionPreviewDialogTests : KitTestContext
{
    private static readonly QuestionResponse Full = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000010"),
        QuestionType.SingleChoice,
        QuestionStatus.Published,
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1"),
        "Crase",
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1"),
        "Português",
        "pt-BR",
        "<p>Qual frase usa a <strong>crase</strong> corretamente?</p>",
        new QuestionBaseTextResponse(
            Guid.Parse("0198f5a0-0000-7000-8000-0000000000d9"),
            "Texto de química",
            "<p>Texto-base de H<sub>2</sub>O e 10<sup>3</sup>.</p>"),
        "<p>Porque <em>à</em> é a fusão da preposição com o artigo.</p>",
        2,
        [
            new(1, "<p>Fui a escola.</p>", false),
            new(0, "<p>Fui <strong>à</strong> escola.</p>", true),
            new(2, "<p>Fui à pé.</p>", false)
        ]);

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAsync(QuestionResponse question, bool unsaved = false)
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, question },
            { dialog => dialog.UnsavedChanges, unsaved }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true }));
        provider.WaitForAssertion(() => provider.FindAll("#question-preview-statement").Should().ContainSingle());

        return provider;
    }

    [Fact]
    public async Task Render_ReadsInTheOrderOfAPaper_BaseTextThenStatementThenTheLetteredOptions()
    {
        var dialog = await OpenAsync(Full);

        var markup = dialog.Markup;
        var baseText = markup.IndexOf("question-preview-base-text", StringComparison.Ordinal);
        var statement = markup.IndexOf("question-preview-statement", StringComparison.Ordinal);
        var options = markup.IndexOf("app-question-options", StringComparison.Ordinal);
        baseText.Should().BeGreaterThan(-1);
        statement.Should().BeGreaterThan(baseText);
        options.Should().BeGreaterThan(statement);

        var list = dialog.Find("ol.app-question-options");
        list.GetAttribute("aria-label").Should().Be("Options");
        list.QuerySelectorAll("li").Should().HaveCount(3);
        dialog.FindAll("li .app-option-letter").Select(letter => letter.TextContent).Should().Equal("A", "B", "C");
    }

    [Fact]
    public async Task Render_OptionsAreOrderedByPosition_NotByTheOrderTheApiSentThem()
    {
        var dialog = await OpenAsync(Full);

        dialog.FindAll("li.app-question-option").Select(item => item.QuerySelector(".app-question-content")!.TextContent.Trim())
            .Should().Equal("Fui à escola.", "Fui a escola.", "Fui à pé.");
    }

    // BR8: the Web shows what the Api stored. The stored markup is rendered as markup, not escaped.
    [Fact]
    public async Task Render_StoredHtml_IsRenderedAsMarkup()
    {
        var dialog = await OpenAsync(Full);

        dialog.Find("#question-preview-statement strong").TextContent.Should().Be("crase");
        dialog.Find("#question-preview-base-text sub").TextContent.Should().Be("2");
        dialog.Find("#question-preview-base-text sup").TextContent.Should().Be("3");
        dialog.Markup.Should().NotContain("&lt;strong&gt;");
    }

    [Fact]
    public async Task Render_TheContentIsMarkedWithItsLanguage()
    {
        var dialog = await OpenAsync(Full);

        dialog.Find(".app-question-paper").GetAttribute("lang").Should().Be("pt-BR");
    }

    [Fact]
    public async Task Render_NoBaseText_ShowsNoBaseTextBlock()
    {
        // F-102 AC11: a question with no linked base text shows none (the inline text is gone).
        var dialog = await OpenAsync(Full with { BaseText = null });

        dialog.FindAll("#question-preview-base-text").Should().BeEmpty();
    }

    // F-102 AC11 (BR9): the linked text is shown before the statement, from the question's base text.
    [Fact]
    public async Task Render_LinkedBaseText_IsShownBeforeTheStatement()
    {
        var dialog = await OpenAsync(Full);

        dialog.Find("#question-preview-base-text").TextContent.Should().Contain("Texto-base de H2O e 103.");
        dialog.Markup.IndexOf("question-preview-base-text", StringComparison.Ordinal)
            .Should().BeLessThan(dialog.Markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }

    // F-102 BR5: a version of the history shows the HTML it recorded, which carries no id of a base text.
    [Fact]
    public async Task Render_AVersion_ShowsTheBaseTextHtmlItRecorded_NotTheLinkedOne()
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, Full with { BaseText = null } },
            { dialog => dialog.VersionRecordedAt, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero) },
            { dialog => dialog.VersionBaseTextHtml, "<p>Texto como era na versão 1.</p>" }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Version 1", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true }));

        provider.WaitForAssertion(() => provider.Find("#question-preview-base-text").TextContent
            .Should().Contain("Texto como era na versão 1."));
    }

    [Fact]
    public async Task Render_BeforeShowAnswer_HidesTheKeyAndTheExplanation()
    {
        var dialog = await OpenAsync(Full);

        dialog.FindAll(".is-correct").Should().BeEmpty();
        dialog.Markup.Should().NotContain("Correct answer").And.NotContain("fusão da preposição");
        var toggle = dialog.Find("#question-preview-toggle");
        toggle.GetAttribute("aria-expanded").Should().Be("false");
        toggle.GetAttribute("aria-controls").Should().Be("question-preview-answer");
        toggle.TextContent.Trim().Should().Be("Show answer");
        dialog.FindAll("#question-preview-answer [role='region']").Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAnswer_MarksTheCorrectOptionWithAnIconATextAndABorder_AndShowsTheExplanation()
    {
        var dialog = await OpenAsync(Full);

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("li.is-correct").Should().ContainSingle());
        var correct = dialog.Find("li.is-correct");
        correct.QuerySelector(".app-option-letter")!.TextContent.Should().Be("A");
        correct.QuerySelector(".app-correct-label")!.TextContent.Should().Contain("Correct answer");
        correct.QuerySelector(".app-correct-label svg").Should().NotBeNull("the key is never colour alone");

        var region = dialog.Find("#question-preview-answer [role='region']");
        var heading = dialog.Find($"#{region.GetAttribute("aria-labelledby")}");
        heading.TextContent.Should().Be("Answer");
        region.TextContent.Should().Contain("Porque").And.Contain("fusão da preposição com o artigo.");
        dialog.Find("#question-preview-answer").GetAttribute("aria-live").Should().Be("polite");

        var toggle = dialog.Find("#question-preview-toggle");
        toggle.GetAttribute("aria-expanded").Should().Be("true");
        toggle.TextContent.Trim().Should().Be("Hide answer");
    }

    [Fact]
    public async Task HideAnswer_HidesTheKeyAgain()
    {
        var dialog = await OpenAsync(Full);
        dialog.Find("#question-preview-toggle").Click();
        dialog.WaitForAssertion(() => dialog.FindAll("li.is-correct").Should().ContainSingle());

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("li.is-correct").Should().BeEmpty());
        dialog.Find("#question-preview-toggle").TextContent.Trim().Should().Be("Show answer");
    }

    [Fact]
    public async Task ShowAnswer_ADraftWithNoKey_SaysNoOptionIsMarked()
    {
        var dialog = await OpenAsync(Full with
        {
            Status = QuestionStatus.Draft,
            Options = [new(0, "<p>A</p>", false), new(1, "<p>B</p>", false)]
        });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.Markup.Should().Contain("This draft has no correct option marked yet."));
        dialog.FindAll("li.is-correct").Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAnswer_NoExplanation_SaysSo()
    {
        var dialog = await OpenAsync(Full with { ExplanationHtml = null });

        dialog.Find("#question-preview-toggle").Click();

        dialog.WaitForAssertion(() => dialog.Markup.Should().Contain("No explanation."));
        dialog.FindAll("#question-preview-explanation").Should().BeEmpty();
    }

    [Fact]
    public async Task Render_WithUnsavedChanges_StartsWithTheNote()
    {
        var dialog = await OpenAsync(Full, unsaved: true);

        dialog.Find(".app-alert").TextContent.Should().Contain("Showing the saved version. Unsaved changes are not included.");
    }

    [Fact]
    public async Task Render_NothingUnsaved_HasNoNote()
    {
        var dialog = await OpenAsync(Full);

        dialog.FindAll(".app-alert").Should().BeEmpty();
    }

    // The ui rule on long text: the content sits in the kit's wrapping container and sets no width of its own.
    [Fact]
    public async Task Render_LongStatement_IsHeldByTheWrappingContainerWithNoFixedWidth()
    {
        var longWord = new string('x', 4000);
        var dialog = await OpenAsync(Full with { StatementHtml = $"<p>{longWord}</p>" });

        var statement = dialog.Find("#question-preview-statement");
        statement.ClassList.Should().Contain("app-question-content");
        statement.TextContent.Should().Be(longWord);
        dialog.FindAll("[style]").Where(element => (element.GetAttribute("style") ?? string.Empty)
            .Contains("width", StringComparison.OrdinalIgnoreCase)).Should().NotContain(element => element.Id == "question-preview-statement");
    }

    [Fact]
    public async Task Close_ClosesTheDialog()
    {
        var dialog = await OpenAsync(Full);

        dialog.Find(".app-preview-close").Click();

        dialog.WaitForAssertion(() => dialog.FindAll("#question-preview-statement").Should().BeEmpty());
    }
}
