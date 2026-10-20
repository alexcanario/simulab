using System.Diagnostics;
using Bunit;
using Bunit.Extensions.WaitForHelpers;
using MudBlazor;

namespace Simulab.Web.Tests.Ui;

/// <summary>B-31: a test process stalled for seconds (a cold start of many test hosts at once) reaches its first check long
/// after the keystroke, so <see cref="KitTestContext.WaitForAfterDebounce{TComponent}"/> has a ceiling of its own, longer
/// than <see cref="BunitContext.DefaultWaitTimeout"/>. A ceiling is a maximum for a failing test: a green run never waits on it.
/// The default wait is static, so these tests run alone (<see cref="SerialDebounceWait"/>) and restore it.</summary>
[Collection(SerialDebounceWait.Name)]
public class DebounceWaitTimeoutTests
{
    // A context whose default wait is short, so that only the debounce ceiling can explain a longer wait.
    private sealed class ShortDefaultContext : KitTestContext
    {
        private readonly TimeSpan _debounceWaitTimeout;

        public ShortDefaultContext(TimeSpan debounceWaitTimeout)
        {
            _debounceWaitTimeout = debounceWaitTimeout;
            DefaultWaitTimeout = TimeSpan.FromMilliseconds(500);
        }

        protected override TimeSpan DebounceWaitTimeout => _debounceWaitTimeout;

        public void WaitAfterDebounce(Action assertion)
        {
            var component = Render<MudPopoverProvider>();
            WaitForAfterDebounce(component, assertion);
        }
    }

    private sealed class SharedContext : KitTestContext
    {
        public TimeSpan Debounce => DebounceWaitTimeout;

        public IRenderedComponent<MudPopoverProvider> RenderPopoverProvider() => Render<MudPopoverProvider>();
    }

    // AC1: the assertion becomes true only after 1.2 s of wall time; the default wait is 500 ms, the debounce ceiling 3 s.
    [Fact]
    public async Task WaitForAfterDebounce_AssertionPassesAfterTheDefaultWait_WaitsForTheDebounceCeiling()
    {
        var previous = BunitContext.DefaultWaitTimeout;
        try
        {
            await using var context = new ShortDefaultContext(TimeSpan.FromSeconds(3));
            var clock = Stopwatch.StartNew();

            context.WaitAfterDebounce(() => clock.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(1200)));

            clock.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(1200));
        }
        finally
        {
            BunitContext.DefaultWaitTimeout = previous;
        }
    }

    // AC2: an assertion that never passes ends at the debounce ceiling, not before and not much after.
    [Fact]
    public async Task WaitForAfterDebounce_AssertionNeverPasses_ThrowsAtTheDebounceCeiling()
    {
        var previous = BunitContext.DefaultWaitTimeout;
        try
        {
            await using var context = new ShortDefaultContext(TimeSpan.FromMilliseconds(1500));
            var clock = Stopwatch.StartNew();

            var act = () => context.WaitAfterDebounce(() => throw new InvalidOperationException("never"));

            act.Should().Throw<WaitForFailedException>();
            clock.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1500));
            clock.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(4));
        }
        finally
        {
            BunitContext.DefaultWaitTimeout = previous;
        }
    }

    // B-32 AC1: the shared context gives every plain wait and the debounced wait the same 15 s ceiling.
    [Fact]
    public async Task SharedContext_Settings_DefaultWaitAndDebounceWaitAre15Seconds()
    {
        var previous = BunitContext.DefaultWaitTimeout;
        try
        {
            await using var context = new SharedContext();

            context.Debounce.Should().Be(TimeSpan.FromSeconds(15));
            BunitContext.DefaultWaitTimeout.Should().Be(TimeSpan.FromSeconds(15));
        }
        finally
        {
            BunitContext.DefaultWaitTimeout = previous;
        }
    }

    // B-32 AC2: a plain wait with no timeout argument whose assertion turns true after 6 s of wall time passes
    // (with the old 5 s ceiling it threw WaitForFailedException).
    [Fact]
    public async Task PlainWaitForAssertion_AssertionPassesAfter6Seconds_WaitsForTheSharedCeiling()
    {
        var previous = BunitContext.DefaultWaitTimeout;
        try
        {
            await using var context = new SharedContext();
            var component = context.RenderPopoverProvider();
            var clock = Stopwatch.StartNew();

            component.WaitForAssertion(() => clock.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(6)));

            clock.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(6));
        }
        finally
        {
            BunitContext.DefaultWaitTimeout = previous;
        }
    }
}
