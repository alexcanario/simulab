using MudBlazor;
using MudBlazor.Services;

namespace Simulab.Web.Components.Ui;

public static class UiKitServiceCollectionExtensions
{
    public const int SnackbarDurationMs = 4000;

    /// <summary>MudBlazor with the app-wide defaults, plus the kit services. Snackbar settings live only here.</summary>
    public static IServiceCollection AddUiKit(this IServiceCollection services)
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.VisibleStateDuration = SnackbarDurationMs;
            config.SnackbarConfiguration.ShowCloseIcon = true;
        });
        services.AddScoped<IConfirmService, ConfirmService>();
        services.AddScoped<ErrorText>();
        services.AddScoped<ThemeState>();
        return services;
    }
}
