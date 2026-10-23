using MudBlazor;
using MudBlazor.Services;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-134: a browser viewport of a given width in pixels; subscribers are told immediately.</summary>
public sealed class FixedWidthViewport(int width) : IBrowserViewportService
{
    public ResizeOptions ResizeOptions { get; } = new();

    public Task SubscribeAsync(IBrowserViewportObserver observer, bool fireImmediately = true) =>
        observer.NotifyBrowserViewportChangeAsync(Args());

    public Task SubscribeAsync(Guid observerId, Action<BrowserViewportEventArgs> lambda, ResizeOptions? options = null, bool fireImmediately = true)
    {
        lambda(Args());
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(Guid observerId, Func<BrowserViewportEventArgs, Task> lambda, ResizeOptions? options = null, bool fireImmediately = true) =>
        lambda(Args());

    public Task UnsubscribeAsync(IBrowserViewportObserver observer) => Task.CompletedTask;

    public Task UnsubscribeAsync(Guid observerId) => Task.CompletedTask;

    public Task<bool> IsMediaQueryMatchAsync(string mediaQuery) => Task.FromResult(false);

    public Task<bool> IsBreakpointWithinWindowSizeAsync(Breakpoint breakpoint) => Task.FromResult(false);

    public Task<bool> IsBreakpointWithinReferenceSizeAsync(Breakpoint breakpoint, Breakpoint reference) => Task.FromResult(false);

    public Task<Breakpoint> GetCurrentBreakpointAsync() => Task.FromResult(CurrentBreakpoint());

    public Task<BrowserWindowSize> GetCurrentBrowserWindowSizeAsync() => Task.FromResult(Size());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Breakpoint CurrentBreakpoint() => width switch
    {
        < 600 => Breakpoint.Xs,
        < 960 => Breakpoint.Sm,
        < 1280 => Breakpoint.Md,
        _ => Breakpoint.Lg
    };

    private BrowserWindowSize Size() => new() { Width = width, Height = 800 };

    private BrowserViewportEventArgs Args() => new(Guid.NewGuid(), Size(), CurrentBreakpoint(), isImmediate: true);
}
