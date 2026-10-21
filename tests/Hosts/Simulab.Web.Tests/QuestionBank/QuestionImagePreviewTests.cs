using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>F-103 AC15 (UC3): the preview shows the pictures of the statement and the options, from the Web route, with their alternative text.</summary>
public sealed class QuestionImagePreviewTests : KitTestContext
{
    private static readonly Guid StatementImage = Guid.Parse("0199a3b4-1111-7222-8333-444455556666");
    private static readonly Guid OptionImage = Guid.Parse("0199a3b4-1111-7222-8333-777788889999");

    private static readonly QuestionResponse WithPictures = new(
        Guid.Parse("0198f5a0-0000-7000-8000-000000000010"),
        QuestionType.SingleChoice,
        QuestionStatus.Published,
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1"),
        "Maps",
        Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1"),
        "Geography",
        "pt-BR",
        $"<p>Look at the map:</p><img src=\"/question-images/{StatementImage}\" alt=\"The map of Brazil\">",
        null,
        null,
        2,
        [
            new(0, $"<img src=\"/question-images/{OptionImage}\" alt=\"A circle\">", true),
            new(1, "<p>A square</p>", false)
        ]);

    [Fact]
    public async Task Preview_ShowsThePicturesOfTheStatementAndTheOptions_FromTheWebRoute_WithTheirAlt()
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<QuestionPreviewDialog>
        {
            { dialog => dialog.Question, WithPictures },
            { dialog => dialog.UnsavedChanges, false }
        };

        await provider.InvokeAsync(async () => await dialogs.ShowAsync<QuestionPreviewDialog>(
            "Preview", parameters, new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true }));

        provider.WaitForAssertion(() => provider.FindAll(".app-question-content img").Should().HaveCount(2));
        var pictures = provider.FindAll(".app-question-content img");
        pictures.Select(picture => picture.GetAttribute("src")).Should().Equal(
            $"/question-images/{StatementImage}", $"/question-images/{OptionImage}");
        pictures.Select(picture => picture.GetAttribute("alt")).Should().Equal("The map of Brazil", "A circle");
    }
}
