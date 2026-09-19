namespace Simulab.Web.Components.Ui;

/// <summary>
/// An extra row action (after edit and delete). The label comes from resources. With a
/// <paramref name="DisabledReason"/> the button is shown disabled and its tooltip says why (F-9).
/// </summary>
public sealed record AppRowAction(string Label, string Icon, Func<Task> OnClick, bool Destructive = false, string? DisabledReason = null);
