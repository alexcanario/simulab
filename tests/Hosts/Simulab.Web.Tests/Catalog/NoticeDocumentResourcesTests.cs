using System.Globalization;
using System.Resources;
using Simulab.Catalog.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-89 AC14: every text of the notice PDF field, the student link and the three new error codes exists in en, pt-BR
/// and pt-PT. <c>ResourceParityTests</c> checks that no key is missing in a language; this pins what this item needs.
/// </summary>
public sealed class NoticeDocumentResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "ExamEditions.NoticeDocument.Label", "ExamEditions.NoticeDocument.Hint",
        "ExamEditions.NoticeDocument.Choose", "ExamEditions.NoticeDocument.Replace",
        "ExamEditions.NoticeDocument.Remove", "ExamEditions.NoticeDocument.Discard",
        "ExamEditions.NoticeDocument.Chosen", "ExamEditions.NoticeDocument.RemovePending",
        "ExamEditions.NoticeDocument.KeepFile", "ExamEditions.NoticeDocument.Download.Name",
        "Catalog.Editions.NoticePdf", "Catalog.Editions.NoticePdf.Name",
        CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf,
        CatalogErrorCodes.ExamEditionNoticeDocumentTooLarge,
        CatalogErrorCodes.ExamEditionNoticeDocumentNotFound,
        "storage.not_configured"
    ]);

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_ExistsInTheThreeLanguages(string key)
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace($"'{key}' in {culture}");
        }
    }

    [Fact]
    public void TheNewTexts_SayTheSameThingEnglishAndPortugueseWayInTheKeyWords()
    {
        Manager.GetString("ExamEditions.NoticeDocument.Hint", new CultureInfo("en"))!.Should().Contain("20 MB");
        Manager.GetString("ExamEditions.NoticeDocument.Hint", new CultureInfo("pt-BR"))!.Should().Contain("20 MB");
        Manager.GetString("ExamEditions.NoticeDocument.Hint", new CultureInfo("pt-PT"))!.Should().Contain("20 MB");
        Manager.GetString("Catalog.Editions.NoticePdf", new CultureInfo("pt-BR"))!.Should().Contain("Edital");
        Manager.GetString("Catalog.Editions.NoticePdf", new CultureInfo("pt-PT"))!.Should().Contain("Aviso");
    }
}
