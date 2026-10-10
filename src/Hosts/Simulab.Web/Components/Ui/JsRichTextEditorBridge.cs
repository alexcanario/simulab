using Microsoft.JSInterop;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// The browser side of <see cref="IRichTextEditorBridge"/> (F-95): calls <c>simulabRichText</c> in
/// <c>wwwroot/js/rich-text.js</c>, which sits on the Quill instance that Blazored.TextEditor creates.
/// </summary>
public sealed class JsRichTextEditorBridge(IJSRuntime js) : IRichTextEditorBridge
{
    public bool HostsEditor => true;

    public async ValueTask<IAsyncDisposable> AttachAsync(RichTextEditorOptions options, RichTextEditorCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(callbacks);

        var reference = DotNetObjectReference.Create(callbacks);
        try
        {
            await js.InvokeVoidAsync("simulabRichText.attach", options, reference);
        }
        catch
        {
            reference.Dispose();
            throw;
        }

        return new Attachment(js, options.Id, reference);
    }

    public ValueTask UpdateAsync(RichTextEditorOptions options) =>
        js.InvokeVoidAsync("simulabRichText.update", options);

    public ValueTask SetHtmlAsync(string id, string html) =>
        js.InvokeVoidAsync("simulabRichText.setHtml", id, html);

    public ValueTask FocusAsync(string id) =>
        js.InvokeVoidAsync("simulabRichText.focus", id);

    private sealed class Attachment(
        IJSRuntime js,
        string id,
        DotNetObjectReference<RichTextEditorCallbacks> reference) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await js.InvokeVoidAsync("simulabRichText.detach", id);
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone: there is no browser left to undo anything in.
            }
            finally
            {
                reference.Dispose();
            }
        }
    }
}
