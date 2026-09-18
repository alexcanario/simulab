using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>bUnit context with the app's kit services, rendered in English.</summary>
public abstract class KitTestContext : BunitContext, IAsyncLifetime
{
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    protected KitTestContext()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        Services.AddLocalization();
        Services.AddUiKit();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // MudBlazor services are only IAsyncDisposable: the container must be disposed asynchronously.
    async Task IAsyncLifetime.DisposeAsync()
    {
        CultureInfo.CurrentUICulture = _previousUiCulture;
        CultureInfo.CurrentCulture = _previousCulture;
        await DisposeAsync();
    }
}
