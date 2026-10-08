using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-79 `/admin/subjects`: the list, its area filter, the dialog and delete (AC3, AC7, AC8, AC16).</summary>
public sealed class SubjectsPageTests : SubjectsTestContext
{
    private IRenderedComponent<Subjects> RenderPage() => Render<Subjects>();

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<Subjects> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    // AC3: the list shows name, area (in the reader's language) and the topic count.
    [Fact]
    public void Load_ShowsNameAreaAndTopicCountForEverySubject()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        CellsOf(page, 0).Should().Equal("Direito Constitucional", "Português", "Atualidades");
        CellsOf(page, 1).Should().Equal("Law", "Languages", "No area");
        CellsOf(page, 2).Should().Equal("0", "2", "0");
        page.FindAll("thead th").Select(header => header.TextContent.Trim())
            .Should().StartWith(["Name", "Area", "Topics"]);
    }

    [Fact]
    public void Load_TheNameIsALinkToTheSubjectsPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.FindAll("tbody tr")[1].QuerySelector("a")!.GetAttribute("href")
            .Should().Be($"/admin/subjects/{Portuguese.Id}");
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        var list = Api.Received.Single(call => call.Method == HttpMethod.Get && call.Path == "/api/v1/catalog/subjects");
        list.Query.Should().Contain("page=0").And.Contain("pageSize=25").And.NotContain("areaId").And.NotContain("withoutArea");
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Subjects = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    [Fact]
    public void Load_NoneYet_ShowsTheEmptyStateWithItsAddAction()
    {
        Subjects = [];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No subject registered yet."));
    }

    [Fact]
    public void Search_SendsTheTermToTheServer()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-table-search input").Input("constit");
        AdvanceDebounce();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Query!.Contains("search=constit", StringComparison.Ordinal)));
    }

    // AC7: the area filter reloads the list and travels as its own query parameter.
    [Fact]
    public async Task AreaFilter_SendsTheAreaIdAndShowsOnlyThatArea()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        var filter = page.FindComponents<AppSelectField<Guid?>>().Single(field => field.Instance.Id == "subjects-filter-area");

        await page.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(Law.Id));

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Query!.Contains($"areaId={Law.Id}", StringComparison.Ordinal)));
        page.WaitForAssertion(() => CellsOf(page, 0).Should().Equal("Direito Constitucional"));
    }

    // AC7: "no area" is its own filter, sent as withoutArea and never as an area id.
    [Fact]
    public async Task AreaFilter_NoArea_SendsWithoutAreaAndShowsOnlyThoseWithoutOne()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        var filter = page.FindComponents<AppSelectField<Guid?>>().Single(field => field.Instance.Id == "subjects-filter-area");

        await page.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(Guid.Empty));

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Query!.Contains("withoutArea=true", StringComparison.Ordinal)));
        Api.Received.Where(call => call.Path == "/api/v1/catalog/subjects").Last().Query.Should().NotContain("areaId");
        page.WaitForAssertion(() => CellsOf(page, 0).Should().Equal("Atualidades"));
    }

    // AC2 (screen side): the filter offers every area by its name in the reader's language.
    [Fact]
    public void AreaFilter_OffersAllTheNoAreaChoiceAndEveryAreaByName()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        var filter = page.FindComponents<AppSelectField<Guid?>>().Single(field => field.Instance.Id == "subjects-filter-area");

        filter.Instance.Options.Select(option => option.Text)
            .Should().Equal("All", "No area", "Languages", "Law");
    }

    // AC3: the add dialog sends the name and shows the snackbar.
    [Fact]
    public void Add_ValidData_SendsItAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Informática");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == "/api/v1/catalog/subjects"));
        var sent = FakeCatalogApi.Read<SaveSubjectRequest>(Api.Received.Last(call => call.Method == HttpMethod.Post).Body);
        sent.Name.Should().Be("Informática");
        sent.AreaId.Should().BeNull();
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Subject saved."));
    }

    [Fact]
    public void Add_DialogOffersTheAreaSelectWithTheNoAreaChoiceFirst()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.FindComponent<AppSelectField<Guid?>>().Instance.Options.Select(option => option.Text)
            .Should().Equal("No area", "Languages", "Law");
    }

    // AC4: a name another subject holds shows on the name field, not only at the top.
    [Fact]
    public void Save_NameTaken_ShowsTheMessageOnTheNameField()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.SubjectNameTaken);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Português");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Another subject already has this name"));
        providers.Dialogs.Find("#subject-name").GetAttribute("aria-invalid").Should().Be("true");
    }

    // AC6 (screen side): an area the server refuses lands on the area field.
    [Fact]
    public void Save_AreaInvalid_ShowsTheMessageOnTheAreaField()
    {
        TaxonomyWriteFailure = (HttpStatusCode.BadRequest, CatalogErrorCodes.SubjectAreaInvalid);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name"));
        providers.Dialogs.Find("#subject-name").Change("Informática");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("This area is not on the list."));
    }

    [Fact]
    public void Edit_OpensTheDialogWithTheSubjectAndSendsAPut()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.FindAll("tbody tr")[1].QuerySelectorAll("button")[0].Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#subject-name").GetAttribute("value").Should().Be("Português"));
        providers.Dialogs.Find("#subject-name").Change("Língua Portuguesa");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/catalog/subjects/{Portuguese.Id}"));
        var sent = FakeCatalogApi.Read<SaveSubjectRequest>(Api.Received.Last(call => call.Method == HttpMethod.Put).Body);
        sent.Should().Be(new SaveSubjectRequest("Língua Portuguesa", Languages.Id));
    }

    // AC8: a subject with topics has its delete disabled, with the reason, before the click.
    [Fact]
    public void Load_SubjectWithTopics_HasItsDeleteDisabledWithTheReason()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        var withTopics = page.FindAll("tbody tr")[1].QuerySelector("button[aria-label='Delete: Português']")!;
        var withoutTopics = page.FindAll("tbody tr")[0].QuerySelector("button[aria-label='Delete: Direito Constitucional']")!;

        withTopics.HasAttribute("disabled").Should().BeTrue();
        withoutTopics.HasAttribute("disabled").Should().BeFalse();
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Português")
            .Instance.DeleteDisabledReason.Should().Be("This subject has topics. Delete them first, on the subject's page.");
        page.FindComponents<AppRowActions>().Single(actions => actions.Instance.ItemName == "Atualidades")
            .Instance.DeleteDisabledReason.Should().BeNull();
    }

    // AC8: a subject without topics is deleted after the confirmation.
    [Fact]
    public void Delete_Confirmed_SendsTheDeleteAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button[aria-label='Delete: Direito Constitucional']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Direito Constitucional"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/catalog/subjects/{Constitutional.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Subject deleted."));
    }

    // BR8: a topic added since the list loaded makes the server answer 409; the alert says why.
    [Fact]
    public void Delete_RefusedBecauseItNowHasTopics_ShowsTheMessageAndKeepsTheRow()
    {
        TaxonomyWriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.SubjectHasTopics);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button[aria-label='Delete: Direito Constitucional']").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Direito Constitucional"));
        providers.Dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This subject has topics."));
        page.FindAll("tbody tr").Should().HaveCount(3);
    }
}
