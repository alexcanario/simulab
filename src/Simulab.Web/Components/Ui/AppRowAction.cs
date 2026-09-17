namespace Simulab.Web.Components.Ui;

/// <summary>An extra row action (after edit and delete). The label comes from resources.</summary>
public sealed record AppRowAction(string Label, string Icon, Func<Task> OnClick, bool Destructive = false);
