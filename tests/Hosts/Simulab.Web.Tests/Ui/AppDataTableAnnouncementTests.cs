using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-50: the search box of <see cref="AppDataTable{TItem}"/> has a description and a polite live region that says how
/// many rows a search or a filter left. AC11 (a screen reader) is in the validation script.
/// </summary>
public sealed class AppDataTableAnnouncementTests : KitTestContext
{
    private const string StatusSelector = "[id^=app-table-status-]";

    private readonly List<AppTableQuery> _queries = [];

    private static GallerySample Sample(int number) =>
        new(number, $"Item {number}", "Description", number, number, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private IRenderedComponent<AppDataTable<GallerySample>> RenderTable(
        GallerySource? source = null,
        Func<AppTableQuery, CancellationToken, Task<AppTablePage<GallerySample>>>? load = null,
        string? searchPlaceholder = null,
        bool searchable = true)
    {
        source ??= new GallerySource(Sample, TimeSpan.Zero);
        Render<MudPopoverProvider>();

        return Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.Searchable, searchable)
            .Add(p => p.SearchPlaceholder, searchPlaceholder)
            .Add(p => p.LoadAsync, load ?? (async (query, token) =>
            {
                _queries.Add(query);
                return await source.LoadAsync(query, token);
            }))
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<GallerySample, string>>(0);
                builder.AddComponentParameter(1, nameof(PropertyColumn<GallerySample, string>.Property),
                    (System.Linq.Expressions.Expression<Func<GallerySample, string>>)(x => x.Name));
                builder.CloseComponent();
            }));
    }

    private static string Status(IRenderedComponent<AppDataTable<GallerySample>> table) =>
        table.Find(StatusSelector).TextContent;

    private static Task Search(IRenderedComponent<AppDataTable<GallerySample>> table, string term)
    {
        var search = table.FindComponent<MudTextField<string>>();
        return table.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync(term));
    }

    [Fact]
    public void Render_Always_HasAnEmptyHiddenPoliteStatusRegion()
    {
        var table = RenderTable();

        // AC7: in the page from the first render, empty, and still empty once the first load has ended.
        table.Find(StatusSelector).TextContent.Should().BeEmpty();
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().NotBeEmpty());
        var status = table.Find(StatusSelector);
        status.GetAttribute("role").Should().Be("status");
        status.GetAttribute("aria-live").Should().Be("polite");
        status.ClassList.Should().Contain("app-visually-hidden");
        status.TextContent.Should().BeEmpty();
    }

    [Fact]
    public void Search_TermWithManyMatches_SaysTheTotalNotThePageRows()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        Search(table, "Item 3"); // Item 3, 30-39: 11 rows

        table.WaitForAssertion(() => Status(table).Should().Be("11 results"));
    }

    [Fact]
    public void Search_TotalLargerThanThePage_SaysTheTotal()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        Search(table, "Item"); // 60 matches, 25 on the page

        table.WaitForAssertion(() => Status(table).Should().Be("60 results"));
    }

    [Fact]
    public void Search_OneMatch_SaysOneResult()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        Search(table, "Item 60");

        table.WaitForAssertion(() => Status(table).Should().Be("1 result"));
    }

    [Fact]
    public void Search_NoMatch_SaysNoResults()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        Search(table, "zzz");

        table.WaitForAssertion(() => Status(table).Should().Be("No results"));
    }

    [Fact]
    public async Task ReloadFromFirstPage_AfterAFilterChange_SaysTheNewTotal()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        await table.InvokeAsync(table.Instance.ReloadFromFirstPageAsync);

        table.WaitForAssertion(() => Status(table).Should().Be("60 results"));
    }

    [Fact]
    public async Task ReloadFromFirstPage_SearchableFalse_StillSaysTheTotalAndHasNoDescription()
    {
        var table = RenderTable(searchable: false, searchPlaceholder: "Search by name");
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        await table.InvokeAsync(table.Instance.ReloadFromFirstPageAsync);

        table.WaitForAssertion(() => Status(table).Should().Be("60 results"));
        table.FindAll("[id^=app-table-hint-]").Should().BeEmpty();
    }

    [Fact]
    public void Load_FirstLoadPagingSizeAndSort_AnnounceNothing()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().HaveCount(25));
        Status(table).Should().BeEmpty();

        var next = table.FindAll(".mud-table-pagination-actions button")
            .Single(button => button.GetAttribute("aria-label")?.Contains("Next", StringComparison.OrdinalIgnoreCase) == true);
        next.Click();
        table.WaitForAssertion(() => table.Markup.Should().Contain("Item 26"));
        Status(table).Should().BeEmpty();

        table.Find("th .mud-table-sort-label, th .sortable-column-header, th button")
            .Click();
        table.WaitForAssertion(() => _queries.Last().SortBy.Should().NotBeNull());
        Status(table).Should().BeEmpty();
    }

    [Fact]
    public void Search_AfterOneWasAnnounced_ThenPaging_KeepsTheText()
    {
        var table = RenderTable();
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));
        Search(table, "Item");
        table.WaitForAssertion(() => Status(table).Should().Be("60 results"));

        var next = table.FindAll(".mud-table-pagination-actions button")
            .Single(button => button.GetAttribute("aria-label")?.Contains("Next", StringComparison.OrdinalIgnoreCase) == true);
        next.Click();

        table.WaitForAssertion(() => _queries.Last().Page.Should().Be(1));
        table.WaitForAssertion(() => table.Markup.Should().Contain("Item 26"));
        Status(table).Should().Be("60 results");
    }

    [Fact]
    public void Search_LoadFails_AnnouncesNoCount()
    {
        var source = new GallerySource(Sample, TimeSpan.Zero);
        var table = RenderTable(source);
        table.WaitForAssertion(() => _queries.Should().HaveCount(1));

        source.Mode = GallerySourceMode.Failing;
        Search(table, "Item 3");

        table.WaitForAssertion(() => table.FindAll(".app-state-error").Should().ContainSingle());
        Status(table).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FirstLoadCancelledByASecond_SaysOnlyTheSecondTotal()
    {
        var firstStarted = new TaskCompletionSource();
        var source = new GallerySource(Sample, TimeSpan.Zero);
        var table = RenderTable(load: async (query, token) =>
        {
            if (query.Search == "Item 1")
            {
                // Held until a newer search replaces it: its cancellation is the only way out.
                firstStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            }

            return await source.LoadAsync(query, token);
        });
        table.WaitForAssertion(() => table.FindAll("tbody tr.mud-table-row").Should().NotBeEmpty());

        await Search(table, "Item 1");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Status(table).Should().BeEmpty();
        await Search(table, "Item 60");

        table.WaitForAssertion(() => Status(table).Should().Be("1 result"));
    }

    [Fact]
    public void Render_PlaceholderDiffersFromTheName_DescribesTheBoxWithAHiddenPlaceholderText()
    {
        var table = RenderTable(searchPlaceholder: "Search by name or e-mail");

        var input = table.Find(".app-table-search input");
        input.GetAttribute("aria-label").Should().Be("Search");
        var describedBy = input.GetAttribute("aria-describedby");
        describedBy.Should().NotBeNullOrEmpty();
        var hint = table.Find($"#{describedBy}");
        hint.TextContent.Should().Be("Search by name or e-mail");
        hint.ClassList.Should().Contain("app-visually-hidden");
    }

    [Fact]
    public void Render_NoPlaceholder_HasNoDescribedBy()
    {
        var table = RenderTable();

        table.Find(".app-table-search input").HasAttribute("aria-describedby").Should().BeFalse();
        table.FindAll("[id^=app-table-hint-]").Should().BeEmpty();
    }

    [Fact]
    public void Render_PlaceholderEqualToTheName_HasNoDescribedBy()
    {
        var table = RenderTable(searchPlaceholder: "Search");

        table.Find(".app-table-search input").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Render_TwoTables_GetDistinctIds()
    {
        Render<MudPopoverProvider>();
        var source = new GallerySource(Sample, TimeSpan.Zero);

        IRenderedComponent<AppDataTable<GallerySample>> Create() => Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.SearchPlaceholder, "Search by name")
            .Add(p => p.LoadAsync, source.LoadAsync)
            .Add(p => p.Columns, _ => { }));

        var first = Create();
        var second = Create();

        var ids = new[]
        {
            first.Find(StatusSelector).Id, second.Find(StatusSelector).Id,
            first.Find("[id^=app-table-hint-]").Id, second.Find("[id^=app-table-hint-]").Id
        };
        ids.Should().OnlyHaveUniqueItems();
    }
}
