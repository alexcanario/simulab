namespace Simulab.Web.Components.Ui;

/// <summary>
/// The one seam between the kit and the visual editor in the browser (F-95). <see cref="AppRichTextField"/> talks
/// only to this; the real implementation drives the editor through JavaScript, a test replaces it with a fake that
/// records what the field asked for and types text on demand.
/// </summary>
public interface IRichTextEditorBridge
{
    /// <summary>
    /// True when the bridge puts a real editor in the browser, so the field hosts one. A test double says false and
    /// the field draws its label, toolbar and description alone, with nothing to start in the browser.
    /// </summary>
    bool HostsEditor { get; }

    /// <summary>
    /// Wires the editable region of the editor hosted by <see cref="RichTextEditorOptions.HostId"/>: its attributes,
    /// the toolbar's keys and formats, and the two callbacks. Disposing the result undoes it.
    /// </summary>
    ValueTask<IAsyncDisposable> AttachAsync(RichTextEditorOptions options, RichTextEditorCallbacks callbacks);

    /// <summary>Applies new attributes and state to an editor that is already attached.</summary>
    ValueTask UpdateAsync(RichTextEditorOptions options);

    /// <summary>Replaces what the editor holds, without raising a change (the value came from outside).</summary>
    ValueTask SetHtmlAsync(string id, string html);

    /// <summary>Moves the focus into the editable region.</summary>
    ValueTask FocusAsync(string id);
}
