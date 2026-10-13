namespace Simulab.Web.Components.Ui;

/// <summary>
/// What <see cref="AppRichTextField"/> tells the editor about itself (F-95): the ids that wire the label and the
/// description to the editable region, the language of the content and the state a screen reader announces.
/// The editable region belongs to the editor package, so these are applied to it by the bridge.
/// </summary>
/// <param name="Id">The editable region's id, the one the error summary and the page focus.</param>
/// <param name="HostId">The id of the element that holds the editor, where the bridge looks for it.</param>
/// <param name="LabelId">The label that names the region (<c>aria-labelledby</c>).</param>
/// <param name="DescriptionId">The hint or error that describes it (<c>aria-describedby</c>).</param>
/// <param name="Lang">BCP 47 tag of the content, so a screen reader switches voice; null follows the page.</param>
/// <param name="Placeholder">What the empty editor invites the reader to write.</param>
/// <param name="Required"><c>aria-required</c>.</param>
/// <param name="Invalid"><c>aria-invalid</c>.</param>
/// <param name="Disabled">The region is not editable and says so (<c>aria-disabled</c>).</param>
/// <param name="Compact">An option row: inline formats only, one line to start with.</param>
/// <param name="Html">The content the editor starts with.</param>
public sealed record RichTextEditorOptions(
    string Id,
    string HostId,
    string LabelId,
    string DescriptionId,
    string? Lang,
    string Placeholder,
    bool Required,
    bool Invalid,
    bool Disabled,
    bool Compact,
    string Html);
