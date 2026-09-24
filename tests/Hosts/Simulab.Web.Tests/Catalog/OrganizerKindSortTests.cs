using System.Globalization;
using Bunit;
using Simulab.Web.Components.Pages.Catalog;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// B-15: sorting the Tipo/Kind column sends the reader's own alphabetical order, not the server's
/// stored-name order.
/// </summary>
public sealed class OrganizerKindSortTests : CatalogPageTestContext
{
    [Theory]
    [InlineData("en", "CertifyingBody,ExamBoard,University")] // Certifying body, Exam board, University
    [InlineData("pt-BR", "ExamBoard,CertifyingBody,University")] // Banca examinadora, Certificadora, Universidade
    [InlineData("pt-PT", "CertifyingBody,ExamBoard,University")] // Entidade certificadora, Júri de exame, Universidade
    public void SortByKind_SendsTheReadersAlphabeticalOrderForTheirCulture(string culture, string expectedOrder)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var page = Render<Organizers>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        page.FindAll(".sortable-column-header").Single(header => header.TextContent.Trim() == KindColumnTitle(culture)).Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Query!.Contains("sortBy=kind", StringComparison.Ordinal)));
        var list = Api.Received.Last(call => call.Method == HttpMethod.Get);
        list.Query.Should().Contain($"kindOrder={Uri.EscapeDataString(expectedOrder)}");
    }

    private static string KindColumnTitle(string culture) => culture switch
    {
        "pt-BR" or "pt-PT" => "Tipo",
        _ => "Kind"
    };
}
