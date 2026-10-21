using Bunit;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>F-80 `/admin/exams`: the search box says what it searches (AC10, BR7).</summary>
public sealed class ExamsSearchPlaceholderTests : CatalogPageTestContext
{
    [Fact]
    public void SearchBox_SaysItSearchesExamAuthorityOrPlace()
    {
        var page = Render<Exams>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().NotBeEmpty());

        page.Find(".app-table-search input").GetAttribute("placeholder").Should().Be("Search by exam, authority or place");
    }
}
