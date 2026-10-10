using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-95: the visual editor swapped for a fake (decision of 2026-10-10). The rich text field talks to the editor in
/// the browser only through <see cref="IRichTextEditorBridge"/>; this double records what the field told it and
/// types text on demand, so a test reads the label, the attributes and the toolbar the field drew, and says what
/// the curator typed, without a browser.
/// </summary>
public sealed class FakeRichTextEditorBridge : IRichTextEditorBridge
{
    private readonly Dictionary<string, RichTextEditorOptions> _editors = [];
    private readonly Dictionary<string, RichTextEditorCallbacks> _callbacks = [];

    /// <summary>False: the field draws no editor package, only its label, toolbar and description.</summary>
    public bool HostsEditor => false;

    /// <summary>The editors that are attached now, by the id of their editable region, as last described.</summary>
    public IReadOnlyDictionary<string, RichTextEditorOptions> Editors => _editors;

    /// <summary>Every <c>SetHtmlAsync</c> the fields asked for: a value that came from outside the editor.</summary>
    public List<(string Id, string Html)> Pushed { get; } = [];

    /// <summary>Every editor the field asked to focus.</summary>
    public List<string> Focused { get; } = [];

    public ValueTask<IAsyncDisposable> AttachAsync(RichTextEditorOptions options, RichTextEditorCallbacks callbacks)
    {
        _editors[options.Id] = options;
        _callbacks[options.Id] = callbacks;

        return ValueTask.FromResult<IAsyncDisposable>(new Detachment(this, options.Id, callbacks));
    }

    public ValueTask UpdateAsync(RichTextEditorOptions options)
    {
        var html = _editors.TryGetValue(options.Id, out var current) ? current.Html : options.Html;
        _editors[options.Id] = options with { Html = html };

        return ValueTask.CompletedTask;
    }

    public ValueTask SetHtmlAsync(string id, string html)
    {
        Pushed.Add((id, html));
        _editors[id] = _editors[id] with { Html = html };

        return ValueTask.CompletedTask;
    }

    public ValueTask FocusAsync(string id)
    {
        Focused.Add(id);

        return ValueTask.CompletedTask;
    }

    /// <summary>What the editor holds now.</summary>
    public string HtmlOf(string id) => _editors[id].Html;

    /// <summary>The reader typed <paramref name="html"/> and paused or left the editor: the field's callback runs.</summary>
    public async Task TypeAsync(string id, string html)
    {
        _editors[id] = _editors[id] with { Html = html };
        await _callbacks[id].ChangedAsync(html);
    }

    /// <summary>The reader pressed the link button: the field asks for an address and answers what the editor applies.</summary>
    public Task<string?> PressLinkAsync(string id, string? currentUrl = null) =>
        _callbacks[id].LinkRequestedAsync(currentUrl);

    private sealed class Detachment(FakeRichTextEditorBridge owner, string id, RichTextEditorCallbacks callbacks) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            // A row that moved to another editor keeps its id; only the attachment that owns it may detach it.
            if (owner._callbacks.TryGetValue(id, out var current) && ReferenceEquals(current, callbacks))
            {
                owner._callbacks.Remove(id);
                owner._editors.Remove(id);
            }

            return ValueTask.CompletedTask;
        }
    }
}
