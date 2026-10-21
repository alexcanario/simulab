using Microsoft.JSInterop;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// What the editor calls back into the field (F-95). The browser side reaches these methods through a
/// <see cref="DotNetObjectReference{TValue}"/> under the names <c>Changed</c>, <c>LinkRequested</c> and
/// <c>ImageRequested</c>; a test double calls them directly.
/// </summary>
public sealed class RichTextEditorCallbacks(
    Func<string, Task> onChanged,
    Func<string?, Task<string?>> onLinkRequested,
    Func<Task<RichTextImageChoice?>>? onImageRequested = null)
{
    /// <summary>The reader paused for 300 ms or left the editor: the HTML the editor holds now.</summary>
    [JSInvokable("Changed")]
    public Task ChangedAsync(string html) => onChanged(html);

    /// <summary>The reader pressed the link button. Returns the address to apply, or null to leave the text alone.</summary>
    [JSInvokable("LinkRequested")]
    public Task<string?> LinkRequestedAsync(string? currentUrl) => onLinkRequested(currentUrl);

    /// <summary>The reader pressed the image button (F-103). Returns the stored picture to insert, or null to leave the text alone.</summary>
    [JSInvokable("ImageRequested")]
    public Task<RichTextImageChoice?> ImageRequestedAsync() =>
        onImageRequested is null ? Task.FromResult<RichTextImageChoice?>(null) : onImageRequested();
}
