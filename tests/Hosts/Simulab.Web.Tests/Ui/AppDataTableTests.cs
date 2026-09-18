using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppDataTableTests : KitTestContext
{
    private readonly List<AppTableQuery> _queries = [];

    private static GallerySample Sample(int number) =>
        new(number, $"Item {number}", "Description", number, number, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private IRenderedComponent<AppDataTable<GallerySample>> RenderTable(GallerySource source)
    {
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
            })
            .Add(p => p.RowActions, item => builder =>
            {
                builder.OpenComponent<AppRowActions>(0);
                builder.AddComponentParameter(1, nameof(AppRowActions.ItemName), item.Name);
                builder.CloseComponent();
            }));
    }

    [Fact]
    public void LoadAsync_SixtyItems_ShowsTwentyFiveRowsAndPageSizes()
    {
        var table = RenderTable(new GallerySource(Sample, TimeSpan.Zero));

        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(25));
        _queries.Should().ContainSingle().Which.Should().Be(new AppTableQuery(0, 25, null, null, false));
        AppDataTable<GallerySample>.PageSizes.Should().Equal(10, 25, 50);
        table.FindComponent<MudDataGridPager<GallerySample>>().Instance.PageSizeOptions.Should().Equal(10, 25, 50);
        table.Find("table").ClassList.Should().NotBeNull();
        table.FindComponent<MudDataGrid<GallerySample>>().Instance.Hover.Should().BeTrue();
    }

    [Fact]
    public void Paging_NextPage_RequestsSecondPageFromSource()
    {
        var table = RenderTable(new GallerySource(Sample, TimeSpan.Zero));
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        var next = table.FindAll(".mud-table-pagination-actions button").Single(button => button.GetAttribute("aria-label")?.Contains("Next", StringComparison.OrdinalIgnoreCase) == true);
        next.Click();

        table.WaitForAssertion(() => _queries.Last().Page.Should().Be(1));
        table.WaitForAssertion(() => table.Markup.Should().Contain("Item 26"));
    }

    [Fact]
    public void LoadAsync_EmptySource_ShowsEmptyStateWithPrimaryAction()
    {
        var table = RenderTable(new GallerySource(Sample, TimeSpan.Zero) { Mode = GallerySourceMode.Empty });

        table.WaitForAssertion(() => table.FindAll(".app-state-empty").Should().ContainSingle());
        table.Find(".app-empty-action").TextContent.Should().Contain("Add");
    }

    [Fact]
    public void LoadAsync_FailingSource_ShowsErrorStateAndRetryCallsSourceAgain()
    {
        var source = new GallerySource(Sample, TimeSpan.Zero) { Mode = GallerySourceMode.Failing };
        var table = RenderTable(source);
        table.WaitForAssertion(() => table.FindAll(".app-state-error").Should().ContainSingle());

        source.Mode = GallerySourceMode.Normal;
        table.Find(".app-retry").Click();

        table.WaitForAssertion(() => _queries.Should().HaveCount(2));
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(25));
    }

    [Fact]
    public async Task LoadAsync_SlowSource_ShowsLoadingState()
    {
        var release = new TaskCompletionSource();
        Render<MudPopoverProvider>();
        var table = Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.LoadAsync, async (_, _) =>
            {
                await release.Task;
                return new AppTablePage<GallerySample>([], 0);
            })
            .Add(p => p.Columns, _ => { }));

        table.WaitForAssertion(() => table.FindAll(".app-state-loading").Should().ContainSingle());
        await table.InvokeAsync(release.SetResult);
        table.WaitForAssertion(() => table.FindAll(".app-state-loading").Should().BeEmpty());
    }

    [Fact]
    public void Search_TermTyped_SourceReceivesTermOnFirstPage()
    {
        var table = RenderTable(new GallerySource(Sample, TimeSpan.Zero));
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        var search = table.FindComponent<MudTextField<string>>();
        table.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("Item 3"));

        table.WaitForAssertion(() => _queries.Last().Should().Be(new AppTableQuery(0, 25, "Item 3", null, false)));
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(11)); // Item 3, 30-39
    }

    [Fact]
    public void RowActions_Given_RenderedInLastColumn()
    {
        var table = RenderTable(new GallerySource(Sample, TimeSpan.Zero));

        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().NotBeEmpty());
        var firstRow = table.FindAll("tbody tr.mud-table-row")[0];
        var cells = firstRow.QuerySelectorAll("td");
        cells[cells.Length - 1].ClassList.Should().Contain("app-cell-actions");
    }

    [Fact]
    public void NumberAndDateColumns_Rendered_AreRightAlignedAndCultureFormatted()
    {
        Render<MudPopoverProvider>();
        var table = Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.LoadAsync, (_, _) => Task.FromResult(new AppTablePage<GallerySample>(
                [new GallerySample(1, "A", "B", 1234, 56.5m, new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero))], 1)))
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<AppNumberColumn<GallerySample, decimal>>(0);
                builder.AddComponentParameter(1, "Property", (System.Linq.Expressions.Expression<Func<GallerySample, decimal>>)(x => x.Score));
                builder.AddComponentParameter(2, "Format", "N1");
                builder.CloseComponent();
                builder.OpenComponent<AppDateColumn<GallerySample, DateTimeOffset>>(3);
                builder.AddComponentParameter(4, "Property", (System.Linq.Expressions.Expression<Func<GallerySample, DateTimeOffset>>)(x => x.UpdatedAt));
                builder.CloseComponent();
            }));

        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().ContainSingle());
        var cells = table.FindAll("tbody tr.mud-table-row td");
        cells[0].ClassList.Should().Contain(AppNumberColumn<GallerySample, decimal>.CssClass);
        cells[0].TextContent.Should().Contain("56.5");
        cells[1].ClassList.Should().Contain(AppDateColumn<GallerySample, DateTimeOffset>.CssClass);
        cells[1].TextContent.Should().Contain("3/4/2026");
    }
}
