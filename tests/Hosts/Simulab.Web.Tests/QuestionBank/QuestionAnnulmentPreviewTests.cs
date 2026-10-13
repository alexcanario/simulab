using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>F-101 AC17, UC3: the preview of an annulled question carries a note with the date and the reason.</summary>
public sealed class QuestionAnnulmentPreviewTests : KitTestContext
{
    private static readonly QuestionResponse Base = new(
        // A published question with two options; the test below annuls it.
        Guid.Parse("0198f5a0-0000-7000-8000-000000000040"),
        QuestionType.SingleChoice,
        QuestionStatus.Published,
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1"),
        "Crase",
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1"),
        "Português",
        "pt-BR",
        "<p>Qual frase usa a crase corretamente?</p>",
        null,
        null,
        null,
        [new(0, "<p>Fui à escola.</p>", true), new(1, "<p>Fui a escola.</p>", false)]);

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAsync(QuestionResponse question)
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog> { { dialog => dialog.Question, question } };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true }));
        provider.WaitForAssertion(() => provider.FindAll("#question-preview-statement").Should().ContainSingle());

        return provider;
    }

    [Fact]
    public async Task Preview_OfAnAnnulledQuestion_ShowsTheDateAndTheReasonAboveTheStatement()
    {
        var annulled = Base with
        {
            Status = QuestionStatus.Annulled,
            Annulment = new QuestionAnnulmentResponse("A banca anulou o item 12.", new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero))
        };

        var dialog = await OpenAsync(annulled);

        var note = dialog.Find("#question-preview-annulment");
        note.TextContent.Should().Contain("Annulled on").And.Contain("A banca anulou o item 12.");
        dialog.Markup.IndexOf("question-preview-annulment", StringComparison.Ordinal)
            .Should().BeLessThan(dialog.Markup.IndexOf("question-preview-statement", StringComparison.Ordinal));
    }
}
