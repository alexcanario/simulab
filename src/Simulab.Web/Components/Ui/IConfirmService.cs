namespace Simulab.Web.Components.Ui;

/// <summary>Asks the user to confirm an action with the kit dialog.</summary>
public interface IConfirmService
{
    /// <summary>Returns true only when the user pressed the confirm button.</summary>
    Task<bool> ConfirmAsync(ConfirmRequest request);
}
