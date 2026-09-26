namespace Simulab.Web.Components.Ui;

/// <summary>One line of an <see cref="AppFormAside"/>'s checklist (F-43, BR1): what is still missing before a
/// save can succeed.</summary>
/// <param name="Label">What has to be filled, already translated.</param>
/// <param name="Done">Whether it is filled.</param>
public sealed record AppChecklistItem(string Label, bool Done);
