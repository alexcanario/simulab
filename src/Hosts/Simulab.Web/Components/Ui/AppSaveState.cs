namespace Simulab.Web.Components.Ui;

/// <summary>What <see cref="AppSaveStatus"/> says (F-169): nothing yet, a save in flight, a saved answer, a cleared one.</summary>
public enum AppSaveState
{
    None,
    Saving,
    Saved,
    Cleared
}
