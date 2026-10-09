using Microsoft.AspNetCore.Components;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-75: the remove button of an <see cref="AppChip"/>. The accessible name has no default and no way to be left
/// out: the wording depends on the page ("Remove Mathematics"), so a chip that is removable cannot be built without
/// it (rule: ui).
/// </summary>
/// <param name="Label">The button's tooltip and accessible name, already translated.</param>
/// <param name="OnRemove">What the button does.</param>
/// <param name="ButtonId">The button's id, when a page must move the focus to it.</param>
/// <param name="Disabled">True while the page is saving.</param>
public sealed record AppChipRemove(string Label, EventCallback OnRemove, string? ButtonId = null, bool Disabled = false);
