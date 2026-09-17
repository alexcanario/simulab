using MudBlazor;
using MudBlazor.Services;

namespace Simulab.Web.Tests.Layout;

/// <summary>A browser viewport with a fixed breakpoint; subscribers are told immediately.</summary>
public sealed class FakeViewport(Breakpoint breakpoint) : IBrowserViewportService
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

    public Task<Breakpoint> GetCurrentBreakpointAsync() => Task.FromResult(breakpoint);

    public Task<BrowserWindowSize> GetCurrentBrowserWindowSizeAsync() => Task.FromResult(Size());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private BrowserWindowSize Size() => new() { Width = breakpoint >= Breakpoint.Md ? 1280 : 400, Height = 800 };

    private BrowserViewportEventArgs Args() => new(Guid.NewGuid(), Size(), breakpoint, isImmediate: true);
}
