namespace Simulab.Web.Components.Ui;

/// <summary>One line of an <see cref="AppFormAside"/> (F-43, BR1): a field's label and what it holds right now.</summary>
/// <param name="Label">The field's label, already translated.</param>
/// <param name="Value">What is filled in, or null while it is empty.</param>
public sealed record AppAsideRow(string Label, string? Value);
