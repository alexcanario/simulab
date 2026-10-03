using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-33 `/admin/organizers`: the list, the dialog and delete (AC3, AC5, AC6, AC9, AC11).</summary>
public sealed class OrganizersPageTests : CatalogPageTestContext
{
    private IRenderedComponent<Organizers> RenderPage() => Render<Organizers>();

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<Organizers> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    // AC3: name, acronym and kind, in the reader's words.
    [Fact]
    public void Load_ShowsNameAcronymAndTheKindTranslated()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        CellsOf(page, 0).Should().Equal("Centro Brasileiro de Pesquisa em Avaliacao", "Fundacao Getulio Vargas", "International Organization for Standardization");
        CellsOf(page, 1).Should().Equal("CEBRASPE", "FGV", "ISO");
        CellsOf(page, 2).Should().Equal("Exam board", "University", "Certifying body");
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        var list = Api.Received.Single(call => call.Method == HttpMethod.Get);
        list.Path.Should().Be("/api/v1/catalog/organizers");
        list.Query.Should().Contain("page=0").And.Contain("pageSize=25");
    }

    [Fact]
    public void Search_SendsTheTermToTheServer()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("input.app-table-search input, .app-table-search input").Input("fgv");
        AdvanceDebounce();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Query!.Contains("search=fgv", StringComparison.Ordinal)));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.Organizers = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    [Fact]
    public void Load_NoOrganizerYet_ShowsTheEmptyStateWithItsAddAction()
    {
        Api.Organizers = [];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No organizer has been registered yet."));
    }

    // AC5: what the dialog sends is what the reader typed, with the acronym uppercased by the Api.
    [Fact]
    public void Add_ValidData_SendsItAndShowsTheSnackbar()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#organizer-name").Should().ContainSingle());
        dialogs.Find("#organizer-name").Change("  Vunesp  ");
        dialogs.Find("#organizer-acronym").Change("vnsp");
        dialogs.Find("#organizer-website").Change("https://vunesp.com.br");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var created = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Post).Subject;
            var sent = FakeCatalogApi.Read<SaveOrganizerRequest>(created.Body);
            sent.Name.Should().Be("Vunesp", "the dialog trims before it sends");
            sent.Acronym.Should().Be("vnsp");
            sent.Kind.Should().Be(nameof(OrganizerKind.ExamBoard), "the kind travels as its name and the first one is the default");
            sent.Website.Should().Be("https://vunesp.com.br");
            sent.Description.Should().BeNull("a blank optional field is sent as nothing");
        });
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Organizer saved."));
    }

    // AC9: editing sends a PUT on that organizer's id and the dialog opens filled.
    [Fact]
    public void Edit_OpensFilledAndSendsAPutOnThatOrganizer()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.FindAll("tbody tr")[1].QuerySelectorAll("button.app-row-action")[0].Click();
        dialogs.WaitForAssertion(() => dialogs.Find("#organizer-name").GetAttribute("value").Should().Be("Fundacao Getulio Vargas"));
        dialogs.Find("#organizer-acronym").GetAttribute("value").Should().Be("FGV");
        dialogs.Find("#organizer-name").Change("Fundacao Getulio Vargas Norte");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() =>
        {
            var saved = Api.Received.Should().ContainSingle(call => call.Method == HttpMethod.Put).Subject;
            saved.Path.Should().Be($"/api/v1/catalog/organizers/{Fgv.Id}");
            FakeCatalogApi.Read<SaveOrganizerRequest>(saved.Body).Name.Should().Be("Fundacao Getulio Vargas Norte");
        });
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Organizer saved."));
    }

    // AC6: a conflict lands on the field that caused it, not on a banner at the top.
    [Fact]
    public void Save_NameTaken_ShowsTheMessageOnTheNameField()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.OrganizerNameTaken);
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#organizer-name").Should().ContainSingle());
        dialogs.Find("#organizer-name").Change("Fundacao Getulio Vargas");
        dialogs.Find("#organizer-acronym").Change("FGV2");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Another organizer already has this name."));
        dialogs.FindAll("#organizer-name").Should().ContainSingle("the dialog stays open so the name can be changed");
    }

    [Fact]
    public void Save_AcronymTaken_ShowsTheMessageOnTheAcronymField()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.OrganizerAcronymTaken);
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#organizer-name").Should().ContainSingle());
        dialogs.Find("#organizer-name").Change("Another board");
        dialogs.Find("#organizer-acronym").Change("FGV");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Another organizer already has this acronym."));
    }

    // AC11, comfort side: the page refuses the address before the Api is called at all.
    [Fact]
    public void Save_WebsiteThatIsNotAnAbsoluteAddress_IsRefusedWithoutCallingTheApi()
    {
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#organizer-name").Should().ContainSingle());
        dialogs.Find("#organizer-name").Change("Vunesp");
        dialogs.Find("#organizer-acronym").Change("VNSP");
        dialogs.Find("#organizer-website").Change("vunesp.com.br");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Type a full address, starting with http:// or https://"));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Save_BlankName_IsRefusedWithoutCallingTheApi()
    {
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#organizer-name").Should().ContainSingle());
        dialogs.Find("#organizer-acronym").Change("VNSP");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Type a name with at least 2 characters."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }
}
