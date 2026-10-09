namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-75: one option of an <see cref="AppMultiPickField"/> list. The page decides whether it is picked and whether it
/// can be picked now (a comfort check; the Api stays the authority), the field only draws it.
/// </summary>
/// <param name="Key">Stable, unique in the field; it is what the field hands back when the option is toggled.</param>
/// <param name="Text">The name as typed (content, never translated).</param>
/// <param name="Picked">True when the option is in the picks (a check mark; <c>aria-selected</c>).</param>
/// <param name="Disabled">True when it cannot be toggled now (<c>aria-disabled</c>); the arrow keys still reach it.</param>
/// <param name="Secondary">
/// A quieter line under the name: the reason when the option is disabled, else what the option stands for ("whole
/// subject"). It is also the option's accessible description.
/// </param>
/// <param name="IsGroupOption">True for the option that stands for its group as a whole (the first of the group).</param>
public sealed record AppMultiPickOption(
    string Key,
    string Text,
    bool Picked,
    bool Disabled = false,
    string? Secondary = null,
    bool IsGroupOption = false);
