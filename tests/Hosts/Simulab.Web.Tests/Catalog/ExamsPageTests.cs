using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-34 `/admin/exams`: the list, its filters and delete (AC2, AC13, AC16).</summary>
public sealed class ExamsPageTests : CatalogPageTestContext
{
    private IRenderedComponent<Exams> RenderPage() => Render<Exams>();

    private static IReadOnlyList<string> CellsOf(IRenderedComponent<Exams> page, int column) =>
        [.. page.FindAll("tbody tr").Select(row => row.QuerySelectorAll("td")[column].TextContent.Trim())];

    // AC2: name, issuing authority, assessment type and scope, in the reader's words.
    [Fact]
    public void Load_ShowsTheNameTheAuthorityAndTheTranslatedColumns()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        CellsOf(page, 0).Should().Equal("Agente de Policia Federal", "FUVEST");
        CellsOf(page, 1).Should().Equal("Policia Federal", "Prefeitura Municipal de Guarulhos");
        page.Markup.Should().NotContain("PF").And.NotContain("PMG", "the authority is named without its acronym (F-44 BR1)");
        CellsOf(page, 2).Should().Equal("Public service exam", "University entrance exam");
        // The scope cell carries its detail under the label when the scope has one (BR8).
        CellsOf(page, 3)[0].Should().Be("National");
        CellsOf(page, 3)[1].Should().Contain("State").And.Contain("Sao Paulo");
    }

    [Fact]
    public void Load_AsksTheServerForTheFirstPage()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        var list = Api.Received.Single(call => call.Method == HttpMethod.Get && call.Path.EndsWith("/exams", StringComparison.Ordinal));
        list.Path.Should().Be("/api/v1/catalog/exams");
        list.Query.Should().Contain("page=0").And.Contain("pageSize=25");
    }

    // AC16: the term goes to the server, which is where the accent-insensitive search runs.
    [Fact]
    public void Search_SendsTheTermToTheServer()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find(".app-table-search input").Input("avaliacao");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Query!.Contains("search=avaliacao", StringComparison.Ordinal)));
    }

    // AC16: a filter reloads the list and travels as its own query parameter. The pick goes through the
    // field's own ValueChanged, which is exactly what choosing an option raises.
    [Fact]
    public async Task Filter_ByAssessmentType_ReloadsTheListWithThatFilter()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        var filter = page.FindComponents<AppSelectField<AssessmentType?>>()
            .Single(field => field.Instance.Id == "exams-filter-type");
        await page.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(AssessmentType.Certification));

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/catalog/exams"
            && call.Query!.Contains($"assessmentType={AssessmentType.Certification}", StringComparison.Ordinal)));
    }

    // BR14: sorting a column whose label is translated sends the reader's own order.
    [Fact]
    public void SortByScope_SendsTheReadersOrderForThatColumn()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll(".sortable-column-header").Single(header => header.TextContent.Trim() == "Scope").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Query!.Contains($"sortBy={ExamSort.Scope}", StringComparison.Ordinal)
            && call.Query.Contains("scopeOrder=", StringComparison.Ordinal)));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.Exams = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    // The empty state names which emptiness it is (BR14).
    [Fact]
    public void Load_NoExamYet_ShowsTheEmptyStateWithItsAddAction()
    {
        Api.Exams = [];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("No exam in the catalog yet."));
    }

    // AC13: the confirmation names the exam and its authority, and the delete goes to that exam's route.
    [Fact]
    public void Delete_Confirmed_SendsTheDeleteAndShowsTheSnackbar()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[1].Click();

        providers.Dialogs.WaitForAssertion(() =>
            providers.Dialogs.Markup.Should().Contain("Agente de Policia Federal").And.Contain("inside Policia Federal.").And.NotContain("PF"));
        providers.Dialogs.FindAll("button").Last(button => button.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Method == HttpMethod.Delete && call.Path == $"/api/v1/catalog/exams/{AgentePf.Id}"));
        providers.Snackbars.WaitForAssertion(() => providers.Snackbars.Markup.Should().Contain("Exam deleted."));
    }

    [Fact]
    public void Delete_Cancelled_SendsNothing()
    {
        var providers = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.FindAll("tbody tr")[0].QuerySelectorAll("button")[1].Click();
        providers.Dialogs.WaitForAssertion(() => providers.Dialogs.Markup.Should().Contain("Agente de Policia Federal"));
        providers.Dialogs.FindAll("button").First(button => button.TextContent.Contains("Cancel", StringComparison.Ordinal)).Click();

        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Delete);
    }

    // BR16 (v2): the authority filter asks the issuing-authority list, never the boards.
    [Fact]
    public void AuthorityFilter_Typing_AsksTheIssuingAuthorityListWithTheTerm()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));

        page.Find("#exams-filter-authority").Input("gua");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call =>
            call.Path == "/api/v1/catalog/issuing-authorities" && call.Query!.Contains("search=gua", StringComparison.Ordinal)));
        Api.Received.Should().NotContain(call => call.Path == "/api/v1/catalog/organizers");
    }
}
