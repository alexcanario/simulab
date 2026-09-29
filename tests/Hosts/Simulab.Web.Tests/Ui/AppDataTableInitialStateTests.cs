using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-36 BR13 and AC17: a table that starts from a search, a page and a page size (a page that keeps its state in the
/// address). Without them it starts as before; that is AppDataTableTests.
/// </summary>
public sealed class AppDataTableInitialStateTests : KitTestContext
{
    private readonly List<AppTableQuery> _queries = [];

    private static GallerySample Sample(int number) =>
        new(number, $"Item {number}", "Description", number, number, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private IRenderedComponent<AppDataTable<GallerySample>> RenderTable(
        string? initialSearch = null,
        int initialPage = 0,
        int? initialPageSize = null)
    {
        var source = new GallerySource(Sample, TimeSpan.Zero);
        Render<MudPopoverProvider>();

        return Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.InitialSearch, initialSearch)
            .Add(p => p.InitialPage, initialPage)
            .Add(p => p.InitialPageSize, initialPageSize)
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
    public void Render_InitialSearchPageAndSize_TheFirstLoadAsksForThemAndTheSearchBoxShowsTheText()
    {
        var table = RenderTable(initialSearch: "  Item 3 ", initialPage: 1, initialPageSize: 10);

        // "Item 3" matches Item 3 and Item 30 to 39: eleven rows, so page 1 of size 10 holds the last one.
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().ContainSingle());
        _queries.First().Should().Be(new AppTableQuery(1, 10, "Item 3", null, false));
        table.Markup.Should().Contain("Item 39");
        table.Find(".app-table-search input").GetAttribute("value").Should().Be("Item 3");
    }

    [Fact]
    public void Render_BlankInitialSearch_IsNoSearch()
    {
        var table = RenderTable(initialSearch: "   ");

        table.WaitForAssertion(() => _queries.Should().NotBeEmpty());
        _queries.First().Search.Should().BeNull();
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(7, 25)]
    [InlineData(100, 25)]
    public void Render_InitialPageSize_IsUsedOnlyWhenTheTableOffersIt(int asked, int used)
    {
        var table = RenderTable(initialPageSize: asked);

        table.WaitForAssertion(() => _queries.Should().NotBeEmpty());
        _queries.First().PageSize.Should().Be(used);
    }

    [Fact]
    public void Render_InitialPagePastTheLastPage_ShowsTheFirstPageInstead()
    {
        // Sixty items at 25 a page: page 9 is past the end, and the total says there are rows.
        var table = RenderTable(initialPage: 9);

        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(25));
        _queries.First().Page.Should().Be(9);
        _queries.Should().Contain(query => query.Page == 0);
        table.FindAll("tbody tr.mud-table-row td")[0].TextContent.Trim().Should().Be("Item 1");
        table.FindAll(".app-state-empty").Should().BeEmpty();
    }

    [Fact]
    public void Render_NoInitialState_StartsOnTheFirstPageOfTwentyFive()
    {
        var table = RenderTable();

        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(25));
        _queries.First().Should().Be(new AppTableQuery(0, 25, null, null, false));
    }
}
