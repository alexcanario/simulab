namespace Simulab.Web.Components.Ui;

/// <summary>What a confirmation dialog shows. Texts come already localized.</summary>
/// <param name="Title">Dialog title.</param>
/// <param name="Message">What happens, including what happens to related data.</param>
/// <param name="ConfirmText">Names the action and the object, e.g. "Delete exam board ABC". Never "Yes".</param>
/// <param name="Destructive">The confirm button uses the error color.</param>
public sealed record ConfirmRequest(string Title, string Message, string ConfirmText, bool Destructive);
