namespace Simulab.Web.Components.Ui;

/// <summary>
/// One line of an <see cref="AppErrorSummary"/> (F-43, BR1): the field that was refused and the id the link
/// jumps to, which is the same id its label already points at.
/// </summary>
/// <param name="FieldId">The input's id, so the link moves focus to the field itself.</param>
/// <param name="Label">The field's label, already translated, so the reader recognises it.</param>
public sealed record AppErrorSummaryItem(string FieldId, string Label);
