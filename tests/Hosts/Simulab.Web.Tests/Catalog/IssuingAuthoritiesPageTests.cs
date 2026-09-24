using System.Net;
using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-34 AC17 (v2) `/admin/issuing-authorities`: the list, the dialog and delete.</summary>
public sealed class IssuingAuthoritiesPageTests : CatalogPageTestContext
{
    private IRenderedComponent<IssuingAuthorities> RenderPage() => Render<IssuingAuthorities>();

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<IssuingAuthorities> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    // AC17: name and acronym, sorted by name, and no kind column — the entity has none.
    [Fact]
    public void Load_ShowsNameAndAcronymAndNoKindColumn()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        CellsOf(page, 0).Should().Equal("Prefeitura Municipal de Guarulhos", "Policia Federal");
        CellsOf(page, 1).Should().Equal("PMG", "PF");
        page.FindAll("thead th").Should().HaveCount(3, "name, acronym and the actions column");
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        var list = Api.Received.Single(call => call.Method == HttpMethod.Get);
        list.Path.Should().Be("/api/v1/catalog/issuing-authorities");
        list.Query.Should().Contain("page=0").And.Contain("pageSize=25");
    }

    [Fact]
    public void Search_SendsTheTermToTheServer()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find(".app-table-search input").Input("guarulhos");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Query!.Contains("search=guarulhos", StringComparison.Ordinal)));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.IssuingAuthorities = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    [Fact]
    public void Load_NoneYet_ShowsTheEmptyStateWithItsAddAction()
    {
        Api.IssuingAuthorities = [];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No issuing authority yet."));
    }

    // AC17: what the dialog sends is what the reader typed, and the Api uppercases the acronym.
    [Fact]
    public void Add_ValidData_SendsItAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find(".app-page-header button").Click();

        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#issuing-authority-name"));
        providers.Dialogs.Find("#issuing-authority-name").Change("Ministerio da Educacao");
        providers.Dialogs.Find("#issuing-authority-acronym").Change("mec");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Post && call.Path == "/api/v1/catalog/issuing-authorities"));
        var sent = FakeCatalogApi.Read<SaveIssuingAuthorityRequest>(
            Api.Received.Last(call => call.Method == HttpMethod.Post).Body);
        sent.Name.Should().Be("Ministerio da Educacao");
        sent.Acronym.Should().Be("mec");
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Issuing authority saved."));
    }

    // AC17: a name another body already holds shows on the name field, not only at the top.
    [Fact]
    public void Save_NameTaken_ShowsTheMessageOnTheNameField()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.IssuingAuthorityNameTaken);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#issuing-authority-name"));
        providers.Dialogs.Find("#issuing-authority-name").Change("Prefeitura Municipal de Guarulhos");
        providers.Dialogs.Find("#issuing-authority-acronym").Change("pmg");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Another issuing authority already has this name."));
        providers.Dialogs.Find("#issuing-authority-name").GetAttribute("aria-invalid").Should().Be("true");
    }

    // The website is checked before the Api is called (comfort only; the Api runs the same check).
    [Fact]
    public void Save_WebsiteThatIsNotAnAbsoluteAddress_IsRefusedWithoutCallingTheApi()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find(".app-page-header button").Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Find("#issuing-authority-name"));
        providers.Dialogs.Find("#issuing-authority-name").Change("Prefeitura de Sao Paulo");
        providers.Dialogs.Find("#issuing-authority-acronym").Change("psp");
        providers.Dialogs.Find("#issuing-authority-website").Change("prefeitura.sp.gov.br");
        providers.Dialogs.Find(".app-form-save").Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Type a full address, starting with http:// or https://"));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Edit_OpensFilledAndSendsAPutOnThatBody()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[0].Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Find("#issuing-authority-name").GetAttribute("value").Should().Be(Guarulhos.Name));
        providers.Dialogs.Find("#issuing-authority-name").Change("Prefeitura de Guarulhos");
        providers.Dialogs.Find(".app-form-save").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Put && call.Path == $"/api/v1/catalog/issuing-authorities/{Guarulhos.Id}"));
    }

    // AC14: the 409 for a body that still has exams lands on the list's alert.
    [Fact]
    public void Delete_RefusedBecauseItHasExams_ShowsTheMessageAndKeepsTheRow()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, CatalogErrorCodes.IssuingAuthorityHasExams);
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[1].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain(Guarulhos.Name));
        providers.Dialogs.FindAll("button").Last(button => button.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("This issuing authority has exams in the catalog."));
        page.FindAll("tbody tr").Should().HaveCount(2);
    }

    [Fact]
    public void Delete_Confirmed_SendsTheDeleteAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[1].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain(Guarulhos.Name));
        providers.Dialogs.FindAll("button").Last(button => button.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/catalog/issuing-authorities/{Guarulhos.Id}"));
        providers.Snackbars.WaitForAssertion(() =>
            providers.Snackbars.Markup.Should().Contain("Issuing authority deleted."));
    }
}
