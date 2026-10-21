using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-103 as far as bUnit reaches: the image button in both toolbars and only where the field takes pictures (AC16, BR1,
/// BR14), the image dialog's checks and its upload (UC1, BR2, BR4) and the way a picture is scaled (AC15, BR15). Pasting
/// and dropping a file, and the editor's own DOM, are checked on screen (validation script).
/// </summary>
public sealed class AppRichTextImageTests : KitTestContext
{
    private const string FieldId = "sample-statement";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static RichTextImageOptions Options(Func<RichTextImageFile, Task<RichTextImageUpload>> upload, long maxBytes = 2 * 1024 * 1024) =>
        new(upload, maxBytes, 250);

    private IRenderedComponent<AppRichTextField> RenderField(bool compact, RichTextImageOptions? images) =>
        Render<AppRichTextField>(parameters => parameters
            .Add(field => field.Id, FieldId)
            .Add(field => field.Label, "Statement")
            .Add(field => field.Placeholder, "Write")
            .Add(field => field.Compact, compact)
            .Add(field => field.Images, images));

    // AC16: the button is in the full and in the compact toolbar, named for a screen reader and never pressed.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_FieldThatTakesPictures_HasTheInsertImageButton_InBothToolbars(bool compact)
    {
        var field = RenderField(compact, Options(_ => Task.FromResult(new RichTextImageUpload("/question-images/x", null))));

        var button = field.Find("[role='toolbar'] button[data-format='image']");

        button.GetAttribute("aria-label").Should().Be("Insert image");
        button.GetAttribute("type").Should().Be("button");
        button.HasAttribute("aria-pressed").Should().BeFalse("it opens a dialog and holds no state");
    }

    // BR1: a field that passes no options, like the explanation, has no image button.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_FieldThatTakesNoPicture_HasNoImageButton(bool compact)
    {
        var field = RenderField(compact, null);

        field.FindAll("[role='toolbar'] button[data-format='image']").Should().BeEmpty();
    }

    // BR14: the editor's callback for the image button answers nothing when the field cannot store a picture.
    [Fact]
    public async Task ImageRequested_WithoutAnImageCallback_AnswersNothing()
    {
        var callbacks = new RichTextEditorCallbacks(_ => Task.CompletedTask, _ => Task.FromResult<string?>(null));

        (await callbacks.ImageRequestedAsync()).Should().BeNull();
    }

    // AC15, BR15: the stylesheet scales a picture of a text to its column and never beyond.
    [Fact]
    public void Stylesheet_ScalesAPictureDownToItsColumn_InTheStoredTextAndInTheEditor()
    {
        AppCssColours.Declaration(".app-question-content img", "max-width").Should().Be("100%");
        AppCssColours.Declaration(".app-question-content img", "height").Should().Be("auto");
        AppCssColours.Declaration(".app-rt-frame .ql-editor img", "max-width").Should().Be("100%");
    }

    // UC1: the dialog needs a file before it sends anything.
    [Fact]
    public async Task Insert_WithNoFile_ShowsTheFileError_AndUploadsNothing()
    {
        var uploads = 0;
        var provider = await OpenAsync(Options(_ =>
        {
            uploads++;
            return Task.FromResult(new RichTextImageUpload("/question-images/x", null));
        }));

        provider.Find(".app-image-dialog-insert").Click();

        provider.WaitForAssertion(() => provider.Find("#richtext-image-file-error").TextContent.Should().Contain("Choose an image"));
        uploads.Should().Be(0);
    }

    // BR4: the alternative text is required unless the picture is marked decorative.
    [Fact]
    public async Task Insert_WithAFileButNoAlternativeText_ShowsTheAltError_AndUploadsNothing()
    {
        var uploads = 0;
        var provider = await OpenAsync(Options(_ =>
        {
            uploads++;
            return Task.FromResult(new RichTextImageUpload("/question-images/x", null));
        }));
        provider.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "map.png", contentType: "image/png"));

        provider.Find(".app-image-dialog-insert").Click();

        provider.WaitForAssertion(() => provider.Find("#richtext-image-alt-error, #richtext-image-alt-description").TextContent
            .Should().Contain("alternative text"));
        uploads.Should().Be(0);
    }

    // UC1: file and alternative text go to the page's upload, and the dialog answers with the stored address.
    [Fact]
    public async Task Insert_WithAFileAndAnAlternativeText_Uploads_AndClosesWithTheStoredPicture()
    {
        RichTextImageFile? sent = null;
        var (provider, result) = await OpenWithResultAsync(Options(file =>
        {
            sent = file;
            return Task.FromResult(new RichTextImageUpload("/question-images/0199a3b4-1111-7222-8333-444455556666", null));
        }));
        provider.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "map.png", contentType: "image/png"));
        provider.WaitForAssertion(() => provider.Find("#richtext-image-preview").GetAttribute("src").Should().StartWith("data:image/png;base64,"));
        provider.Find("#richtext-image-alt").Change("  A map of Brazil  ");

        provider.Find(".app-image-dialog-insert").Click();

        provider.WaitForAssertion(() => result.IsCompleted.Should().BeTrue());
        var choice = (RichTextImageChoice)(await result)!.Data!;
        choice.Should().Be(new RichTextImageChoice("/question-images/0199a3b4-1111-7222-8333-444455556666", "A map of Brazil"));
        sent!.Name.Should().Be("map.png");
        sent.Content.Should().Equal(Png);
    }

    // BR4: decorative means an empty alternative text, and the text field is not asked.
    [Fact]
    public async Task Insert_MarkedDecorative_NeedsNoAlternativeText_AndAnswersAnEmptyOne()
    {
        var (provider, result) = await OpenWithResultAsync(Options(_ =>
            Task.FromResult(new RichTextImageUpload("/question-images/0199a3b4-1111-7222-8333-444455556666", null))));
        provider.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "line.png", contentType: "image/png"));
        provider.WaitForAssertion(() => provider.FindAll("#richtext-image-preview").Should().ContainSingle());
        provider.Find("#richtext-image-decorative").Change(true);
        provider.WaitForAssertion(() => provider.Find("#richtext-image-alt").HasAttribute("disabled").Should().BeTrue());

        provider.Find(".app-image-dialog-insert").Click();

        provider.WaitForAssertion(() => result.IsCompleted.Should().BeTrue());
        ((RichTextImageChoice)(await result)!.Data!).Alt.Should().BeEmpty();
    }

    // The Api's refusal is shown under the file field, and the dialog stays open.
    [Fact]
    public async Task Insert_WhenTheUploadIsRefused_ShowsTheMessageUnderTheFile_AndStaysOpen()
    {
        var (provider, result) = await OpenWithResultAsync(Options(_ =>
            Task.FromResult(new RichTextImageUpload(null, "The file is not a PNG, JPEG, WebP or GIF image."))));
        provider.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "a.png", contentType: "image/png"));
        provider.WaitForAssertion(() => provider.FindAll("#richtext-image-preview").Should().ContainSingle());
        provider.Find("#richtext-image-alt").Change("Alt");

        provider.Find(".app-image-dialog-insert").Click();

        provider.WaitForAssertion(() => provider.Find("#richtext-image-file-error").TextContent.Should().Contain("not a PNG, JPEG, WebP or GIF"));
        result.IsCompleted.Should().BeFalse();
    }

    // BR2: a file over the limit is refused before it is read, with the stable code's text.
    [Fact]
    public async Task Choose_AFileOverTheLimit_IsRefusedBeforeItIsRead()
    {
        var provider = await OpenAsync(Options(_ => Task.FromResult(new RichTextImageUpload("/question-images/x", null)), maxBytes: 4));
        provider.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Png, "big.png", contentType: "image/png"));

        provider.WaitForAssertion(() => provider.Find("#richtext-image-file-error").TextContent.Should().Contain("larger than 2 MB"));
        provider.FindAll("#richtext-image-preview").Should().BeEmpty();
    }

    private async Task<IRenderedComponent<MudDialogProvider>> OpenAsync(RichTextImageOptions options) =>
        (await OpenWithResultAsync(options)).Provider;

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, Task<DialogResult?> Result)> OpenWithResultAsync(RichTextImageOptions options)
    {
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<AppRichTextImageDialog> { { dialog => dialog.Options, options } };

        IDialogReference? reference = null;
        await provider.InvokeAsync(async () =>
            reference = await dialogs.ShowAsync<AppRichTextImageDialog>("Insert image", parameters, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true }));

        return (provider, reference!.Result);
    }
}
