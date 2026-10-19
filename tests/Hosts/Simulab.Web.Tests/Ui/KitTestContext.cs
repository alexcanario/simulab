using System.Diagnostics;
using System.Globalization;
using Bunit;
using Bunit.Extensions.WaitForHelpers;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
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

    /// <summary>F-95: the visual editor of every rich text field, swapped for a fake. A test types into it with
    /// <see cref="FakeRichTextEditorBridge.TypeAsync"/> and reads what the fields asked of it.</summary>
    protected FakeRichTextEditorBridge RichText { get; } = new();

    protected KitTestContext()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(Clock);
        Services.AddUiKit();
        Services.AddSingleton<IRichTextEditorBridge>(RichText);
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

    /// <summary>B-30: waits for what a debounced field sends. A keystroke can reach its debounce after a single
    /// <see cref="AdvanceDebounce"/> already moved the clock, and then no other advance would ever come: this moves the
    /// clock again before every try, so a delay registered late is still reached. bUnit re-checks only when the
    /// component renders, so each try is a short wait of its own and the next try follows even when nothing rendered.
    /// The ceiling is <see cref="BunitContext.DefaultWaitTimeout"/>, as for any wait.</summary>
    protected void WaitForAfterDebounce<TComponent>(IRenderedComponent<TComponent> component, Action assertion, int milliseconds = AppLookupField.DebounceMilliseconds)
        where TComponent : IComponent
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            AdvanceDebounce(milliseconds);
            try
            {
                component.WaitForAssertion(assertion, TryWindow);
                return;
            }
            catch (WaitForFailedException) when (elapsed.Elapsed < DefaultWaitTimeout)
            {
                // Not yet: the delay may not have existed when the clock moved. Move it again.
            }
        }
    }

    private static readonly TimeSpan TryWindow = TimeSpan.FromMilliseconds(50);

    public Task InitializeAsync() => Task.CompletedTask;

    // MudBlazor services are only IAsyncDisposable: the container must be disposed asynchronously.
    async Task IAsyncLifetime.DisposeAsync()
    {
        CultureInfo.CurrentUICulture = _previousUiCulture;
        CultureInfo.CurrentCulture = _previousCulture;
        await DisposeAsync();
    }
}
