using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;
using Simulab.Web.Localization;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-36 `/catalog`: the published exams, the filters and the address that keeps them (UC1 to UC3, AC16).</summary>
public sealed class CatalogSearchPageTests : StudentCatalogTestContext
{
    private const string ClearFilters = "#catalog-clear-filters";

    private IRenderedComponent<CatalogSearch> RenderPage() => Render<CatalogSearch>();

    private static AppSelectField<T> Filter<T>(IRenderedComponent<CatalogSearch> page, string id) =>
        page.FindComponents<AppSelectField<T>>().Single(field => field.Instance.Id == id).Instance;

    private static AppLookupField StateFilter(IRenderedComponent<CatalogSearch> page) =>
        page.FindComponents<AppLookupField>().Single(field => field.Instance.Id == "catalog-filter-state").Instance;

    private static Task PickStateAsync(IRenderedComponent<CatalogSearch> page, string? displayName) =>
        page.InvokeAsync(() => StateFilter(page).ValueChanged.InvokeAsync(
            displayName is null ? null : new AppLookupOption(Guid.Empty, displayName)));

    private static void WaitForRows(IRenderedComponent<CatalogSearch> page, int count) =>
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(count));

    // Renders the page with real parameters: the header, the table, its seven columns and the four filters.
    [Fact]
    public void Render_ShowsTheHeaderWithNoActionAndTheSevenColumnsWithNoSort()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find("h1").TextContent.Should().Be("Catalog");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Study").And.Contain("Catalog");
        page.FindAll(".app-primary-action").Should().BeEmpty();
        page.FindAll("thead th").Select(header => header.TextContent.Trim()).Should().Equal(
            "Name", "Issuing authority", "Assessment type", "Scope", "Language", "Editions", "Latest year");
        page.FindAll(".sortable-column-header").Should().BeEmpty();
        page.FindAll("tbody tr button").Should().BeEmpty();
        page.FindComponents<AppSelectField<AssessmentType?>>().Should().ContainSingle();
        page.FindComponents<AppSelectField<ExamScope?>>().Should().ContainSingle();
        page.FindComponents<AppSelectField<Guid?>>().Should().ContainSingle();
        page.FindComponents<AppSelectField<int?>>().Should().ContainSingle();
    }

    // UC1 and BR2: one row per exam, the name is the row's link, the counts are numbers with no group separator on the year.
    [Fact]
    public void Load_ShowsOneRowPerExamWithTheNameAsALinkAndTheTranslatedColumns()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        var cells = page.FindAll("tbody tr")[0].QuerySelectorAll("td");
        var link = cells[0].QuerySelector("a")!;
        link.GetAttribute("href").Should().Be($"/catalog/exams/{GuardaMunicipal.Id}");
        link.GetAttribute("lang").Should().Be("pt-BR");
        link.TextContent.Should().Be("Guarda Municipal");
        cells[1].TextContent.Trim().Should().Be("Prefeitura de Sao Paulo");
        cells[2].TextContent.Trim().Should().Be("Public service exam");
        cells[3].TextContent.Should().Contain("State").And.Contain("São Paulo (SP)", "a State exam reads its state by name and acronym (F-42 AC6)");
        cells[4].TextContent.Trim().Should().Be(SupportedCultures.NativeName(new System.Globalization.CultureInfo("pt-BR")));
        cells[4].QuerySelector("span")!.GetAttribute("lang").Should().Be("pt-BR");
        cells[5].TextContent.Trim().Should().Be("2");
        cells[6].TextContent.Trim().Should().Be("2025");
        cells[5].ClassList.Should().Contain(AppNumberColumn<PublishedExamResponse, int>.CssClass);
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPageAndTheFilterOptionsOnce()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        page.WaitForAssertion(() => Api.Received.Count(call => call.Path == "/api/v1/catalog/published-exam-filters").Should().Be(1));
        Api.ListQueries.Should().ContainSingle().Which.Should().Contain("page=0").And.Contain("pageSize=25");
    }

    // AC16: the search, the scope and the page come from the address; the first load asks for them.
    [Fact]
    public void Open_WithSearchScopeAndPage_RestoresThemAndKeepsTheAddress()
    {
        Api.Exams = Many(30);
        OpenAt("/catalog?search=guarda&scope=State&page=2");

        var page = RenderPage();

        WaitForRows(page, 5);
        Api.ListQueries.First().Should().Contain("search=guarda").And.Contain("scope=State").And.Contain("page=1").And.Contain("pageSize=25");
        page.Find(".app-table-search input").GetAttribute("value").Should().Be("guarda");
        Filter<ExamScope?>(page, "catalog-filter-scope").Value.Should().Be(ExamScope.State);
        page.FindAll(ClearFilters).Should().ContainSingle();
        Navigation.Uri.Should().EndWith("/catalog?search=guarda&scope=State&page=2");
    }

    // AC16 and BR6: what the address says wrongly is no filter, and the address is written back clean.
    [Fact]
    public void Open_WithUnreadableValues_IgnoresThemAndCleansTheAddress()
    {
        OpenAt("/catalog?scope=Bar&assessmentType=Foo&noticeYear=abc&organizerId=nope&page=0&pageSize=7");

        var page = RenderPage();

        WaitForRows(page, 2);
        var query = Api.ListQueries.First();
        query.Should().NotContain("scope=").And.NotContain("assessmentType=").And.NotContain("noticeYear=").And.NotContain("organizerId=");
        query.Should().Contain("page=0").And.Contain("pageSize=25");
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog"));
        page.FindAll(ClearFilters).Should().BeEmpty();
    }

    // BR13: the seven keys, and only those, go on the exam link, so the exam page can bring the list back as it was.
    [Fact]
    public void Load_ExamLinkCarriesTheCatalogQueryString()
    {
        Api.Exams = [GuardaMunicipal];
        OpenAt("/catalog?scope=State&utm=x");

        var page = RenderPage();

        WaitForRows(page, 1);
        page.Find("tbody a").GetAttribute("href").Should().Be($"/catalog/exams/{GuardaMunicipal.Id}?scope=State");
    }

    // BR13: typing, filtering and paging never add history entries, so Back from the list leaves the catalog.
    [Fact]
    public async Task Filter_WritesTheAddressWithoutAddingHistory()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        await page.InvokeAsync(() => Filter<AssessmentType?>(page, "catalog-filter-type").ValueChanged.InvokeAsync(AssessmentType.Certification));

        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog?assessmentType=Certification"));
        Services.GetRequiredService<BunitNavigationManager>().History.Should().NotBeEmpty()
            .And.OnlyContain(entry => entry.Options.ReplaceHistoryEntry);
    }

    // UC3: a filter starts from the first page, narrows the list and shows "Clear filters"; clearing brings the list back.
    [Fact]
    public async Task Filter_ByType_ReloadsFromTheFirstPageAndClearBringsTheListBack()
    {
        var page = RenderPage();
        WaitForRows(page, 2);
        page.FindAll(ClearFilters).Should().BeEmpty();

        await page.InvokeAsync(() => Filter<AssessmentType?>(page, "catalog-filter-type").ValueChanged.InvokeAsync(AssessmentType.Certification));

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("assessmentType=Certification").And.Contain("page=0"));
        WaitForRows(page, 1);
        page.Find("tbody tr td a").TextContent.Should().Be("TOEFL iBT");

        page.Find(ClearFilters).Click();

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().NotContain("assessmentType="));
        WaitForRows(page, 2);
        page.FindAll(ClearFilters).Should().BeEmpty();
        Filter<AssessmentType?>(page, "catalog-filter-type").Value.Should().BeNull();
    }

    [Fact]
    public void Search_SendsTheTextAsTypedAndKeepsItInTheAddress()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(".app-table-search input").Input("guarda sp");
        AdvanceDebounce();

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("search=guarda%20sp"));
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog?search=guarda%20sp"));
    }

    // BR7: the board and year options are the Api's, in its order: "ACRONYM - Name", years as plain digits.
    [Fact]
    public async Task Filter_ByBoardAndYear_OffersTheApisOptionsAndSendsBothChoices()
    {
        var page = RenderPage();
        WaitForRows(page, 2);
        page.WaitForAssertion(() => Filter<Guid?>(page, "catalog-filter-organizer").Options.Should().HaveCount(3));

        Filter<Guid?>(page, "catalog-filter-organizer").Options.Select(option => option.Text).Should().Equal(
            "All", "CONSULPLAN — Instituto Consulplan", "FGV — Fundacao Getulio Vargas");
        Filter<int?>(page, "catalog-filter-year").Options.Select(option => option.Text).Should().Equal("All", "2026", "2025");
        Filter<Guid?>(page, "catalog-filter-organizer").Disabled.Should().BeFalse();
        Filter<int?>(page, "catalog-filter-year").Disabled.Should().BeFalse();

        await page.InvokeAsync(() => Filter<Guid?>(page, "catalog-filter-organizer").ValueChanged.InvokeAsync(Fgv.Id));
        await page.InvokeAsync(() => Filter<int?>(page, "catalog-filter-year").ValueChanged.InvokeAsync(2025));

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain($"organizerId={Fgv.Id}").And.Contain("noticeYear=2025"));
    }

    // While the options call has not answered, the two selects are disabled and offer only "All".
    [Fact]
    public async Task Load_WhileTheOptionsLoad_BoardAndYearAreDisabled()
    {
        Api.HoldFilters = new TaskCompletionSource();

        var page = RenderPage();
        WaitForRows(page, 2);

        Filter<Guid?>(page, "catalog-filter-organizer").Disabled.Should().BeTrue();
        Filter<int?>(page, "catalog-filter-year").Disabled.Should().BeTrue();
        Filter<Guid?>(page, "catalog-filter-organizer").Options.Should().ContainSingle();
        Filter<AssessmentType?>(page, "catalog-filter-type").Disabled.Should().BeFalse();

        await page.InvokeAsync(() => Api.HoldFilters!.SetResult());

        page.WaitForAssertion(() => Filter<Guid?>(page, "catalog-filter-organizer").Disabled.Should().BeFalse());
        Filter<int?>(page, "catalog-filter-year").Disabled.Should().BeFalse();
    }

    // The options failed: the list, the search and the other filters keep working, and Try again asks only for the options.
    [Fact]
    public void Load_WhenTheOptionsFail_ShowsTheWarningAndTryAgainReloadsOnlyThem()
    {
        Api.Filters = null;

        var page = RenderPage();
        WaitForRows(page, 2);

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should()
            .Contain("We could not load the board, year and state options. The rest of the search works."));
        StateFilter(page).Disabled.Should().BeTrue("AC9: the state options come with the board and year ones");
        Filter<Guid?>(page, "catalog-filter-organizer").Disabled.Should().BeTrue();
        Filter<int?>(page, "catalog-filter-year").Disabled.Should().BeTrue();
        Filter<Guid?>(page, "catalog-filter-organizer").Options.Should().ContainSingle();
        Filter<AssessmentType?>(page, "catalog-filter-type").Disabled.Should().BeFalse();
        var listCalls = Api.ListQueries.Count();

        Api.Filters = new PublishedExamFiltersResponse([Fgv], [2025], ["SP"]);
        page.Find(".app-alert-action").Click();

        page.WaitForAssertion(() => page.FindAll(".app-alert").Should().BeEmpty());
        Filter<Guid?>(page, "catalog-filter-organizer").Disabled.Should().BeFalse();
        Api.ListQueries.Count().Should().Be(listCalls);
    }

    // BR6: a board the loaded options do not know is dropped, and the list reloads without it.
    [Fact]
    public void Open_WithABoardTheOptionsDoNotKnow_DropsItAndReloads()
    {
        var unknown = Guid.NewGuid();
        OpenAt($"/catalog?organizerId={unknown}");

        var page = RenderPage();

        page.WaitForAssertion(() =>
        {
            Api.ListQueries.Last().Should().NotContain("organizerId=");
            Navigation.Uri.Should().EndWith("/catalog");
            Filter<Guid?>(page, "catalog-filter-organizer").Value.Should().BeNull();
        });
    }

    // While the options have not answered, a readable board from the address is still sent, and "Clear filters" shows.
    [Fact]
    public void Open_WithABoardWhileTheOptionsLoad_SendsItAndOffersClear()
    {
        Api.HoldFilters = new TaskCompletionSource();
        OpenAt($"/catalog?organizerId={Fgv.Id}");

        var page = RenderPage();

        WaitForRows(page, 2);
        Api.ListQueries.First().Should().Contain($"organizerId={Fgv.Id}");
        page.FindAll(ClearFilters).Should().ContainSingle();
    }

    [Fact]
    public void Load_NothingPublished_ShowsTheEmptyCatalogWithNoAction()
    {
        Api.Exams = [];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No exam is published in the catalog yet."));
        page.FindAll(".app-empty-action").Should().BeEmpty();
    }

    [Fact]
    public void Search_NoMatch_SaysWhichTextFoundNothing()
    {
        var page = RenderPage();
        WaitForRows(page, 2);

        page.Find(".app-table-search input").Input("zzz");
        AdvanceDebounce();

        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No published exam matches \"zzz\"."));
        page.FindAll(".app-empty-action").Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_NoMatch_OffersToClearTheFilters()
    {
        Api.Exams = [GuardaMunicipal];
        var page = RenderPage();
        WaitForRows(page, 1);

        await page.InvokeAsync(() => Filter<AssessmentType?>(page, "catalog-filter-type").ValueChanged.InvokeAsync(AssessmentType.Certification));

        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No published exam matches the chosen filters."));
        page.Find(".app-empty-action").TextContent.Should().Contain("Clear filters");

        page.Find(".app-empty-action").Click();

        WaitForRows(page, 1);
        page.FindAll(ClearFilters).Should().BeEmpty();
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.Exams = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Find(".app-state-error").TextContent.Should().Contain("We could not load this list."));
        page.Find(".app-retry").TextContent.Should().Contain("Try again");
    }

    // F-57 AC2 (screen side): the state field offers only the states the Api says have a published State exam,
    // in the Api's order, and all of them when the field opens (empty term).
    [Fact]
    public async Task StateFilter_OffersOnlyTheStatesTheApiListed_InListOrder()
    {
        Api.Filters = new PublishedExamFiltersResponse([Fgv], [2025], ["RJ", "SP"]);
        var page = RenderPage();
        WaitForRows(page, 2);
        page.WaitForAssertion(() => StateFilter(page).Disabled.Should().BeFalse());

        var options = await StateFilter(page).SearchAsync(string.Empty, CancellationToken.None);

        options.Select(option => option.Text).Should().Equal("Rio de Janeiro (RJ)", "São Paulo (SP)");
        // Typing narrows by name, accent-insensitive.
        (await StateFilter(page).SearchAsync("sao", CancellationToken.None)).Select(option => option.Text)
            .Should().Equal("São Paulo (SP)");
        (await StateFilter(page).SearchAsync("MG", CancellationToken.None)).Should().BeEmpty("MG has no published exam");
    }

    // F-57 AC1: picking a state asks the Api for it, sets the scope to State and keeps the state in the address.
    [Fact]
    public async Task StateFilter_Pick_SendsTheStateSetsTheScopeAndWritesTheAddress()
    {
        var page = RenderPage();
        WaitForRows(page, 2);
        page.WaitForAssertion(() => StateFilter(page).Disabled.Should().BeFalse());

        await PickStateAsync(page, "São Paulo (SP)");

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("state=SP").And.Contain("scope=State").And.Contain("page=0"));
        WaitForRows(page, 1);
        page.Find("tbody tr td a").TextContent.Should().Be("Guarda Municipal");
        Filter<ExamScope?>(page, "catalog-filter-scope").Value.Should().Be(ExamScope.State);
        StateFilter(page).Value!.Text.Should().Be("São Paulo (SP)");
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog?scope=State&state=SP"));
        page.FindAll(ClearFilters).Should().ContainSingle();
    }

    // F-57 AC4: another scope empties the state; clearing only the state leaves the scope.
    [Fact]
    public async Task StateFilter_ScopeChangedAwayFromState_ClearsTheState_ClearingTheStateKeepsTheScope()
    {
        var page = RenderPage();
        WaitForRows(page, 2);
        page.WaitForAssertion(() => StateFilter(page).Disabled.Should().BeFalse());
        await PickStateAsync(page, "São Paulo (SP)");
        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("state=SP"));

        await page.InvokeAsync(() => Filter<ExamScope?>(page, "catalog-filter-scope").ValueChanged.InvokeAsync(ExamScope.National));

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("scope=National").And.NotContain("state="));
        StateFilter(page).Value.Should().BeNull();

        await PickStateAsync(page, "São Paulo (SP)");
        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("state=SP").And.Contain("scope=State"));
        await page.InvokeAsync(() => Filter<ExamScope?>(page, "catalog-filter-scope").ValueChanged.InvokeAsync(null));

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().NotContain("scope=").And.NotContain("state="));
        StateFilter(page).Value.Should().BeNull("all scopes leaves no state");

        await PickStateAsync(page, "São Paulo (SP)");
        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("state=SP"));
        await PickStateAsync(page, null);

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().NotContain("state=").And.Contain("scope=State"));
        Filter<ExamScope?>(page, "catalog-filter-scope").Value.Should().Be(ExamScope.State);
    }

    // F-57 AC5: the state in the address is restored, shown and sent; an address with an unknown state loads
    // unfiltered by state, with no error, and is written back clean.
    [Fact]
    public void Open_WithAState_RestoresItAndSendsIt()
    {
        OpenAt("/catalog?scope=State&state=sp");

        var page = RenderPage();

        WaitForRows(page, 1);
        Api.ListQueries.First().Should().Contain("state=SP");
        StateFilter(page).Value!.Text.Should().Be("São Paulo (SP)");
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog?scope=State&state=SP"));
    }

    [Fact]
    public void Open_WithAStateOffTheList_LoadsWithoutItAndCleansTheAddress()
    {
        OpenAt("/catalog?state=XX");

        var page = RenderPage();

        WaitForRows(page, 2);
        Api.ListQueries.First().Should().NotContain("state=");
        StateFilter(page).Value.Should().BeNull();
        page.FindAll(".app-state-error").Should().BeEmpty();
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog"));
    }

    // F-57 BR4, BR6: a state of the list that no published exam has (a stale link) is dropped once the options
    // arrive, and the list reloads without it.
    [Fact]
    public void Open_WithAStateNoPublishedExamHas_DropsItAndReloads()
    {
        OpenAt("/catalog?scope=State&state=MG");

        var page = RenderPage();

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().NotContain("state="));
        StateFilter(page).Value.Should().BeNull();
        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith("/catalog?scope=State"));
    }

    // F-57 AC6: the state combines with the type and the text; "Clear filters" empties the state too.
    [Fact]
    public async Task StateFilter_CombinesWithTheOtherFilters_AndClearFiltersEmptiesIt()
    {
        var page = RenderPage();
        WaitForRows(page, 2);
        page.WaitForAssertion(() => StateFilter(page).Disabled.Should().BeFalse());
        await page.InvokeAsync(() => Filter<AssessmentType?>(page, "catalog-filter-type").ValueChanged.InvokeAsync(AssessmentType.PublicServiceExam));
        page.Find(".app-table-search input").Input("guarda");
        AdvanceDebounce();
        page.WaitForAssertion(() => Api.ListQueries.Last().Should().Contain("search=guarda"));
        await PickStateAsync(page, "São Paulo (SP)");

        page.WaitForAssertion(() => Api.ListQueries.Last().Should()
            .Contain("state=SP").And.Contain("assessmentType=PublicServiceExam").And.Contain("search=guarda"));

        page.Find(ClearFilters).Click();

        page.WaitForAssertion(() => Api.ListQueries.Last().Should().NotContain("state=").And.NotContain("assessmentType="));
        StateFilter(page).Value.Should().BeNull();
        Filter<ExamScope?>(page, "catalog-filter-scope").Value.Should().BeNull();
    }

    // F-57 BR13 (F-36): the state goes on the exam link with the other keys, so the exam page brings the list back as it was.
    [Fact]
    public void Load_ExamLinkCarriesTheState()
    {
        Api.Exams = [GuardaMunicipal];
        OpenAt("/catalog?scope=State&state=SP");

        var page = RenderPage();

        WaitForRows(page, 1);
        page.Find("tbody a").GetAttribute("href").Should().Be($"/catalog/exams/{GuardaMunicipal.Id}?scope=State&state=SP");
    }

    // F-57 AC9 (the field while the options load): it is disabled with board and year, and the page renders it with its real parameters.
    [Fact]
    public async Task StateFilter_WhileTheOptionsLoad_IsDisabled()
    {
        Api.HoldFilters = new TaskCompletionSource();

        var page = RenderPage();
        WaitForRows(page, 2);

        StateFilter(page).Disabled.Should().BeTrue();
        page.Find("#catalog-filter-state").Should().NotBeNull();

        await page.InvokeAsync(() => Api.HoldFilters!.SetResult());

        page.WaitForAssertion(() => StateFilter(page).Disabled.Should().BeFalse());
    }
}
