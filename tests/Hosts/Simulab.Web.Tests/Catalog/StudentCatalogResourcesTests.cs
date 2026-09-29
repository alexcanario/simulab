using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-36 BR12 and AC15: every text of the two student screens exists in en, pt-BR and pt-PT with the words the item
/// gives. ResourceParityTests checks that no key is missing in a language; this pins what each one says.
/// </summary>
public sealed class StudentCatalogResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Texts() => new()
    {
        { "Nav.Catalog", "Catalog", "Catálogo", "Catálogo" },
        { "Catalog.Title", "Catalog", "Catálogo", "Catálogo" },
        { "Catalog.Search.Placeholder", "Exam, authority or place", "Exame, órgão ou local", "Exame, entidade ou local" },
        { "Catalog.Filter.Organizer", "Board", "Banca", "Entidade organizadora" },
        { "Catalog.Filter.NoticeYear", "Notice year", "Ano do edital", "Ano do aviso" },
        { "Catalog.Filter.Clear", "Clear filters", "Limpar filtros", "Limpar filtros" },
        {
            "Catalog.Filter.OptionsFailed",
            "We could not load the board and year options. The rest of the search works.",
            "Não foi possível carregar as opções de banca e ano. O resto da busca funciona.",
            "Não foi possível carregar as opções de entidade organizadora e ano. O resto da pesquisa funciona."
        },
        { "Catalog.Column.ContentLanguage", "Language", "Idioma", "Idioma" },
        { "Catalog.Column.Editions", "Editions", "Edições", "Edições" },
        { "Catalog.Column.LatestYear", "Latest year", "Último ano", "Último ano" },
        {
            "Catalog.Empty",
            "No exam is published in the catalog yet.",
            "Ainda não há exames publicados no catálogo.",
            "Ainda não há exames publicados no catálogo."
        },
        {
            "Catalog.Empty.Search",
            "No published exam matches \"{0}\".",
            "Nenhum exame publicado corresponde a \"{0}\".",
            "Nenhum exame publicado corresponde a \"{0}\"."
        },
        {
            "Catalog.Empty.Filters",
            "No published exam matches the chosen filters.",
            "Nenhum exame publicado corresponde aos filtros escolhidos.",
            "Nenhum exame publicado corresponde aos filtros escolhidos."
        },
        { "Catalog.Exam.Title", "Exam", "Exame", "Exame" },
        { "Catalog.Exam.About", "About this exam", "Sobre este exame", "Sobre este exame" },
        { "Catalog.Exam.PublishedEditions", "Published editions", "Edições publicadas", "Edições publicadas" },
        { "Catalog.Exam.LatestNoticeYear", "Latest notice year", "Último ano de edital", "Último ano de aviso" },
        {
            "Catalog.Exam.LoadFailed",
            "We could not load this exam.",
            "Não foi possível carregar este exame.",
            "Não foi possível carregar este exame."
        },
        { "Catalog.BackToCatalog", "Back to the catalog", "Voltar para o catálogo", "Voltar ao catálogo" },
        {
            "Catalog.Editions.Subtitle",
            "Each paper applied, newest first. The official notice has the full rules.",
            "Cada prova aplicada, da mais recente para a mais antiga. O edital oficial traz as regras completas.",
            "Cada prova aplicada, da mais recente para a mais antiga. O aviso oficial contém as regras completas."
        },
        {
            "Catalog.Editions.Empty",
            "This exam has no published edition.",
            "Este exame não tem edições publicadas.",
            "Este exame não tem edições publicadas."
        },
        { "Catalog.Editions.NoticeLink", "Official notice", "Edital oficial", "Aviso oficial" },
        {
            "Catalog.Editions.NoticeLink.Name",
            "Official notice, {0}, opens in a new tab",
            "Edital oficial, {0}, abre em uma nova aba",
            "Aviso oficial, {0}, abre num novo separador"
        }
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords(string key, string en, string ptBr, string ptPt)
    {
        Manager.GetString(key, new CultureInfo("en")).Should().Be(en, $"'{key}' in en");
        Manager.GetString(key, new CultureInfo("pt-BR")).Should().Be(ptBr, $"'{key}' in pt-BR");
        Manager.GetString(key, new CultureInfo("pt-PT")).Should().Be(ptPt, $"'{key}' in pt-PT");
    }

    // The roles back office lists catalog.browse (UC7): it needs a name and a description in every language.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void TheBrowsePermission_HasANameAndADescription(string culture)
    {
        Manager.GetString("Permission.catalog.browse.Name", new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace();
        Manager.GetString("Permission.catalog.browse.Description", new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace();
    }
}
