using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-100 AC11: a page that clears its own filters with one button clears the table's search box with them, and the
/// table reloads from the first page with no search.
/// </summary>
public sealed class AppDataTableClearSearchTests : KitTestContext
{
    private readonly List<AppTableQuery> _queries = [];

    private static GallerySample Sample(int number) =>
        new(number, $"Item {number}", "Description", number, number, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private IRenderedComponent<AppDataTable<GallerySample>> RenderTable(string? initialSearch = null)
    {
        var source = new GallerySource(Sample, TimeSpan.Zero);
        Render<MudPopoverProvider>();

        return Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.InitialSearch, initialSearch)
            .Add(p => p.LoadAsync, async (query, token) =>
            {
                _queries.Add(query);
                return await source.LoadAsync(query, token);
            })
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<GallerySample, string>>(0);
                builder.AddComponentParameter(1, nameof(PropertyColumn<GallerySample, string>.Property),
                    (System.Linq.Expressions.Expression<Func<GallerySample, string>>)(x => x.Name));
                builder.CloseComponent();
            }));
    }

    [Fact]
    public async Task ClearSearch_EmptiesTheBox_AndReloadsFromTheFirstPageWithNoSearch()
    {
        var table = RenderTable(initialSearch: "Item 3");
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().NotBeEmpty());
        table.Find(".app-table-search input").GetAttribute("value").Should().Be("Item 3");
        _queries.Clear();

        await table.InvokeAsync(() => table.Instance.ClearSearchAsync());

        table.WaitForAssertion(() => _queries.Should().NotBeEmpty());
        _queries.Last().Search.Should().BeNull();
        _queries.Last().Page.Should().Be(0);
        table.WaitForAssertion(() => table.Find(".app-table-search input").GetAttribute("value").Should().BeNullOrEmpty());
    }

    [Fact]
    public async Task ClearSearch_WithNoSearch_ReloadsAndChangesNothingElse()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().NotBeEmpty());
        _queries.Clear();

        await table.InvokeAsync(() => table.Instance.ClearSearchAsync());

        table.WaitForAssertion(() => _queries.Should().NotBeEmpty());
        _queries.Last().Search.Should().BeNull();
    }
}
