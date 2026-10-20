using Bunit;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-89 UC4 on <c>/catalog/exams/{id}</c>: beside the official link, a PDF download for the editions that have one.</summary>
public sealed class CatalogExamNoticePdfTests : StudentCatalogTestContext
{
    private static readonly PublishedExamEditionResponse WithFile = Edition2025 with
    {
        NoticeDocument = new NoticeDocumentResponse("edital-2025.pdf", 1_048_576, new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero))
    };

    private IRenderedComponent<CatalogExam> RenderPage()
    {
        var editions = new[] { WithFile, Edition2023 };
        Api.Details[GuardaMunicipal.Id] = new PublishedExamDetailResponse(
            GuardaMunicipal.Id,
            GuardaMunicipal.Name,
            GuardaMunicipal.IssuingAuthorityName,
            GuardaMunicipal.AssessmentType,
            GuardaMunicipal.Scope,
            GuardaMunicipal.ScopeDetail,
            GuardaMunicipal.ContentLanguage,
            editions.Length,
            2025,
            editions);
        var page = Render<CatalogExam>(parameters => parameters.Add(component => component.Id, GuardaMunicipal.Id));
        page.WaitForAssertion(() => page.FindAll("li.app-item-row").Should().HaveCount(2));
        return page;
    }

    // AC11: the link points at the Web's download route, says it is a PDF, and names the edition for a screen reader.
    [Fact]
    public void Render_AnEditionWithAFile_ShowsTheNoticePdfDownloadLink()
    {
        var page = RenderPage();

        var link = page.FindAll("li.app-item-row")[0].QuerySelector("a[href$='/notice-document']");

        link.Should().NotBeNull();
        link!.GetAttribute("href").Should().Be($"/catalog/published-exams/{GuardaMunicipal.Id}/editions/{WithFile.Id}/notice-document");
        link.HasAttribute("download").Should().BeTrue("the browser saves the file instead of navigating");
        link.TextContent.Should().Contain("Notice (PDF)");
        link.GetAttribute("aria-label").Should().StartWith("Notice (PDF)").And.Contain("2025").And.Contain("download");
    }

    [Fact]
    public void Render_AnEditionWithoutAFile_ShowsNoPdfLink()
    {
        var page = RenderPage();

        var rows = page.FindAll("li.app-item-row");
        rows[0].QuerySelectorAll("a[href$='/notice-document']").Should().ContainSingle("only the edition that has a file shows the link");
        rows[1].QuerySelectorAll("a[href$='/notice-document']").Should().BeEmpty();
    }

    // The official link and the PDF are independent: an edition with both shows both.
    [Fact]
    public void Render_AnEditionWithBoth_ShowsTheOfficialLinkAndThePdfLink()
    {
        var page = RenderPage();

        var row = page.FindAll("li.app-item-row")[0];
        row.QuerySelectorAll("a[href='https://www.consulplan.net/edital-2025']").Should().ContainSingle();
        row.QuerySelectorAll("a[href$='/notice-document']").Should().ContainSingle();
    }
}
