using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>bUnit context with the app's kit services, rendered in English.</summary>
public abstract class KitTestContext : BunitContext, IAsyncLifetime
{
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    /// <summary>AuthorizeView (F-5's UserMenu, in the app bar every layout renders) needs this even when
    /// a test has nothing to do with sign-in. Defaults to an anonymous visitor; a test can call
    /// <c>Authorization.SetAuthorized(...)</c> to render the signed-in state instead.</summary>
    protected BunitAuthorizationContext Authorization { get; }

    /// <summary>F-48: the clock MudBlazor's debounce reads from the container. A test that types into a search box or a
    /// lookup field moves it with <see cref="AdvanceDebounce"/> instead of waiting for 300 ms of wall clock.</summary>
    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));

    protected KitTestContext()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(Clock);
        Services.AddUiKit();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // B-19: the kit's search and lookup fields debounce for 300 ms on MudBlazor's own real timer
        // (AppDataTable, AppLookupField), so a WaitFor behind one of them has only 700 ms left of bUnit's
        // 1-second default. Under the whole solution running in parallel that budget runs out and the test
        // fails for the machine's load, not for the code. A timeout is a maximum: a green run does not wait.
        DefaultWaitTimeout = TimeSpan.FromSeconds(5);

        Authorization = AddAuthorization().SetNotAuthorized();
    }

    /// <summary>Lets the kit's debounce elapse (F-48). The default is the lookup's interval, which the table's search box
    /// shares; a field with another interval passes it.</summary>
    protected void AdvanceDebounce(int milliseconds = AppLookupField.DebounceMilliseconds) =>
        Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));

    public Task InitializeAsync() => Task.CompletedTask;

    // MudBlazor services are only IAsyncDisposable: the container must be disposed asynchronously.
    async Task IAsyncLifetime.DisposeAsync()
    {
        CultureInfo.CurrentUICulture = _previousUiCulture;
        CultureInfo.CurrentCulture = _previousCulture;
        await DisposeAsync();
    }
}
