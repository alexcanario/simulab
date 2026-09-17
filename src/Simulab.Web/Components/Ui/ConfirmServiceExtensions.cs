using Microsoft.Extensions.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Ui;

public static class ConfirmServiceExtensions
{
    /// <summary>Destructive confirmation: "Delete {object}", with what happens to related data.</summary>
    public static Task<bool> ConfirmDeleteAsync(this IConfirmService confirm, IStringLocalizer<SharedResources> l, string objectName, string consequence) =>
        confirm.ConfirmAsync(new ConfirmRequest(
            l["Common.Delete.Title"],
            consequence,
            l["Common.Delete.Confirm", objectName],
            Destructive: true));

    /// <summary>Leaving a form with unsaved changes.</summary>
    public static Task<bool> ConfirmDiscardAsync(this IConfirmService confirm, IStringLocalizer<SharedResources> l) =>
        confirm.ConfirmAsync(new ConfirmRequest(
            l["Common.Discard.Title"],
            l["Common.Discard.Message"],
            l["Common.Discard.Confirm"],
            Destructive: true));
}
