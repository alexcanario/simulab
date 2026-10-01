using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Localization;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-36 `/catalog/exams/{id}`: the exam and its published editions, and the ways back to the list (UC4, AC10, AC16).</summary>
public sealed class CatalogExamPageTests : StudentCatalogTestContext
{
    private static PublishedExamDetailResponse Detail(PublishedExamResponse exam, params PublishedExamEditionResponse[] editions) =>
        new(
            exam.Id,
            exam.Name,
            exam.IssuingAuthorityName,
            exam.AssessmentType,
            exam.Scope,
            exam.ScopeDetail,
            exam.ContentLanguage,
            editions.Length,
            editions.Select(edition => edition.NoticeYear).DefaultIfEmpty(0).Max(),
            editions);

    private IRenderedComponent<CatalogExam> RenderPage(Guid id) =>
        Render<CatalogExam>(parameters => parameters.Add(page => page.Id, id));

    private IRenderedComponent<CatalogExam> RenderGuardaMunicipal()
    {
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal, Edition2025, Edition2023);
        var page = RenderPage(GuardaMunicipal.Id);
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().HaveCount(2));
        return page;
    }

    // UC4 and BR8: the exam, and every published edition in the Api's order, with its board, position, reference and date.
    [Fact]
    public void Render_ShowsTheExamAsTheTitleAndItsEditionsInTheApisOrder()
    {
        var page = RenderGuardaMunicipal();

        var title = page.Find("h1");
        title.TextContent.Should().Be("Guarda Municipal");
        title.GetAttribute("lang").Should().Be("pt-BR");
        page.Find("#catalog-exam-editions-title").TextContent.Should().Be("Editions");
        page.Find(".app-form-aside h2").TextContent.Should().Be("About this exam");
        page.FindAll(".app-primary-action").Should().BeEmpty();

        var rows = page.FindAll("li.app-item-row");
        rows[0].TextContent.Should().Contain("2025")
            .And.Contain("Guarda Municipal de 3a Classe")
            .And.Contain("Instituto Consulplan (CONSULPLAN)")
            .And.Contain("Edital 01/2025")
            .And.Contain("Applied on 3/16/2025");
        rows[1].TextContent.Should().Contain("2023").And.Contain("Fundacao Getulio Vargas (FGV)");
        rows[0].QuerySelector("span[lang=pt-BR]")!.TextContent.Should().Be("Guarda Municipal de 3a Classe");
        page.FindAll("li.app-item-row button").Should().BeEmpty();
    }

    // The exam's facts in the kit's read-only summary, in the order the item gives; the place only for State and Municipal.
    [Fact]
    public void Render_TheAsideListsTheExamsFactsInOrder()
    {
        var page = RenderGuardaMunicipal();

        var terms = page.FindAll(".app-form-aside dt").Select(term => term.TextContent.Trim());
        var values = page.FindAll(".app-form-aside dd").Select(value => value.TextContent.Trim());
        terms.Zip(values).Should().Equal(
            ("Issuing authority", "Prefeitura de Sao Paulo"),
            ("Assessment type", "Public service exam"),
            ("Scope", "State"),
            ("State", "Sao Paulo"),
            ("Content language", SupportedCultures.NativeName(new System.Globalization.CultureInfo("pt-BR"))),
            ("Published editions", "2"),
            ("Latest notice year", "2025"));
    }

    [Fact]
    public void Render_ANationalExamHasNoPlaceRowAndItsTitleCarriesItsLanguage()
    {
        Api.Details[Toefl.Id] = Detail(Toefl, Edition2025);
        var page = RenderPage(Toefl.Id);
        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("TOEFL iBT"));

        page.Find("h1").GetAttribute("lang").Should().Be("en");
        page.FindAll(".app-form-aside dt").Select(term => term.TextContent.Trim()).Should().Equal(
            "Issuing authority", "Assessment type", "Scope", "Content language", "Published editions", "Latest notice year");
    }

    // AC10 and BR9: the notice opens in a new tab, and its accessible name starts with the visible text and says so.
    [Fact]
    public void NoticeLink_OpensInANewTabAndSaysSoToAScreenReader()
    {
        var page = RenderGuardaMunicipal();

        var link = page.FindAll("li.app-item-row a").Should().ContainSingle().Which;
        link.GetAttribute("href").Should().Be("https://www.consulplan.net/edital-2025");
        link.GetAttribute("target").Should().Be("_blank");
        link.GetAttribute("rel").Should().Be("noopener noreferrer");
        link.TextContent.Trim().Should().Be("Official notice");
        link.GetAttribute("aria-label").Should().Be(
            "Official notice, 2025, Guarda Municipal de 3a Classe, Instituto Consulplan (CONSULPLAN), opens in a new tab");
        link.GetAttribute("aria-label")!.Should().StartWith(link.TextContent.Trim());
        link.QuerySelector("svg")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    // An edition with nothing but its year and board shows only those: no placeholder, no link, one visible separator.
    [Fact]
    public void Render_ASparseEditionShowsOnlyWhatItHas()
    {
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal, Edition2023);
        var page = RenderPage(GuardaMunicipal.Id);
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().ContainSingle());

        var row = page.Find("li.app-item-row");
        row.TextContent.Should().Contain("2023").And.Contain("Fundacao Getulio Vargas (FGV)").And.NotContain("Applied on");
        row.QuerySelectorAll("a").Should().BeEmpty();
        row.QuerySelectorAll(".app-muted").Should().BeEmpty();
        row.QuerySelectorAll("[aria-hidden=true]").Should().ContainSingle();
    }

    [Fact]
    public void Render_NoPublishedEditionLeft_SaysSo()
    {
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal);
        var page = RenderPage(GuardaMunicipal.Id);

        page.WaitForAssertion(() => page.Find(".app-item-rows-empty").TextContent.Should().Contain("This exam has no published edition."));
    }

    // BR13 and AC16: the breadcrumb's "Catalog" returns to the list as it was: only the seven keys are carried.
    [Fact]
    public void Render_TheBreadcrumbCarriesTheCatalogQueryString()
    {
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal, Edition2025);
        OpenAt($"/catalog/exams/{GuardaMunicipal.Id}?search=guarda&scope=State&page=2&utm=x");

        var page = RenderPage(GuardaMunicipal.Id);

        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().ContainSingle());
        page.FindAll(".app-breadcrumbs a").Select(anchor => anchor.GetAttribute("href"))
            .Should().Contain("/catalog?search=guarda&scope=State&page=2");
        page.Find(".app-breadcrumbs").TextContent.Should().Contain("Study").And.Contain("Guarda Municipal");
    }

    [Fact]
    public void Render_OpenedWithoutTheKeys_TheBreadcrumbGoesToThePlainCatalog()
    {
        var page = RenderGuardaMunicipal();

        page.FindAll(".app-breadcrumbs a").Select(anchor => anchor.GetAttribute("href")).Should().Contain("/catalog");
    }

    // BR8 and BR1: a missing exam, a deleted one and one with only drafts look the same, with a way back.
    [Fact]
    public void Render_UnknownExam_ShowsTheNotFoundAlertAndTheWayBackWithTheCatalogQuery()
    {
        OpenAt("/catalog/exams/0198f0a3-9999-7000-8000-000000000000?scope=State&page=2");

        var page = RenderPage(Guid.Parse("0198f0a3-9999-7000-8000-000000000000"));

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain("This exam no longer exists."));
        page.Find(".app-alert").GetAttribute("role").Should().Be("alert");
        var back = page.Find(".app-form-card a.app-link");
        back.TextContent.Should().Be("Back to the catalog");
        back.GetAttribute("href").Should().Be("/catalog?scope=State&page=2");
        page.Find("h1").TextContent.Should().Be("Exam");
        page.Find("h1").HasAttribute("lang").Should().BeFalse();
        page.FindAll(".app-form-aside").Should().BeEmpty();
    }

    [Fact]
    public void Render_ServerError_ShowsTheErrorStateAndTryAgainLoadsTheExam()
    {
        Api.FindFails = true;
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal, Edition2025);
        var page = RenderPage(GuardaMunicipal.Id);

        page.WaitForAssertion(() => page.Find(".app-state-error").TextContent.Should().Contain("We could not load this exam."));

        Api.FindFails = false;
        page.Find(".app-retry").Click();

        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("Guarda Municipal"));
        page.FindAll(".app-state-error").Should().BeEmpty();
    }

    [Fact]
    public async Task Render_WhileTheExamLoads_ShowsTheGenericTitleAndTheLoadingState()
    {
        Api.HoldFind = new TaskCompletionSource();
        Api.Details[GuardaMunicipal.Id] = Detail(GuardaMunicipal, Edition2025);

        var page = RenderPage(GuardaMunicipal.Id);

        page.WaitForAssertion(() => page.FindAll(".app-state-loading").Should().ContainSingle());
        page.Find("h1").TextContent.Should().Be("Exam");
        page.FindAll(".app-breadcrumbs a").Should().NotBeEmpty();

        await page.InvokeAsync(() => Api.HoldFind!.SetResult());

        page.WaitForAssertion(() => page.Find("h1").TextContent.Should().Be("Guarda Municipal"));
    }
}
