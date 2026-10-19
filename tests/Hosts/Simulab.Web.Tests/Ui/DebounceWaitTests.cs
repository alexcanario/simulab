using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>B-30: a keystroke can reach its debounce after a test moved the fake clock, so one advance can miss it.
/// <see cref="KitTestContext.WaitForAfterDebounce{TComponent}"/> moves the clock on every try.</summary>
public class DebounceWaitTests : KitTestContext
{
    private readonly List<AppTableQuery> _queries = [];

    private static GallerySample Sample(int number) =>
        new(number, $"Item {number}", "Description", number, number, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private IRenderedComponent<AppDataTable<GallerySample>> RenderTable()
    {
        var source = new GallerySource(Sample, TimeSpan.Zero);
        Render<MudPopoverProvider>();
        return Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.LoadAsync, async (query, token) =>
            {
                _queries.Add(query);
                return await source.LoadAsync(query, token);
            })
            .Add(p => p.EmptyActionText, "Add")
            .Add(p => p.OnEmptyAction, () => { })
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<GallerySample, string>>(0);
                builder.AddComponentParameter(1, nameof(PropertyColumn<GallerySample, string>.Property),
                    (System.Linq.Expressions.Expression<Func<GallerySample, string>>)(x => x.Name));
                builder.CloseComponent();
            }));
    }

    // AC1: the clock moves before the box registered its delay (the window of B-30, forced here), and the search still goes out.
    [Fact]
    public void WaitForAfterDebounce_ClockMovedBeforeTheDelayExisted_StillSendsTheSearch()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        Clock.Advance(TimeSpan.FromMilliseconds(AppLookupField.DebounceMilliseconds));
        table.Find(".app-table-search input").Input("Item 3");

        WaitForAfterDebounce(table, () => _queries.Should().HaveCount(2));

        _queries.Last().Should().Be(new AppTableQuery(0, 25, "Item 3", null, false));
    }

    // AC2: a try that does not pass is followed by another advance; the try that passes is the last one.
    [Fact]
    public void WaitForAfterDebounce_AssertionFailsTwice_MovesTheClockOnEveryTryAndStopsWhenItPasses()
    {
        var component = Render<MudPopoverProvider>();
        var start = Clock.GetUtcNow();
        var tries = 0;

        WaitForAfterDebounce(component, () =>
        {
            tries++;
            tries.Should().BeGreaterThanOrEqualTo(3);
        });

        tries.Should().Be(3);
        Clock.GetUtcNow().Should().Be(start + TimeSpan.FromMilliseconds(3 * AppLookupField.DebounceMilliseconds));
    }

    [Fact]
    public void WaitForAfterDebounce_AnotherInterval_MovesTheClockByThatInterval()
    {
        var component = Render<MudPopoverProvider>();
        var start = Clock.GetUtcNow();

        WaitForAfterDebounce(component, () => { }, 500);

        Clock.GetUtcNow().Should().Be(start + TimeSpan.FromMilliseconds(500));
    }
}
