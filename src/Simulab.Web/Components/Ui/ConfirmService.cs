using MudBlazor;

namespace Simulab.Web.Components.Ui;

internal sealed class ConfirmService(IDialogService dialogs) : IConfirmService
{
    internal static readonly DialogOptions Options = new()
    {
        CloseOnEscapeKey = true,
        BackdropClick = false,
        MaxWidth = MaxWidth.ExtraSmall,
        FullWidth = true,
    };

    public async Task<bool> ConfirmAsync(ConfirmRequest request)
    {
        var parameters = new DialogParameters<AppConfirmDialog>
        {
            { d => d.Message, request.Message },
            { d => d.ConfirmText, request.ConfirmText },
            { d => d.Destructive, request.Destructive },
        };

        var dialog = await dialogs.ShowAsync<AppConfirmDialog>(request.Title, parameters, Options);
        var result = await dialog.Result;
        return result is { Canceled: false };
    }
}
