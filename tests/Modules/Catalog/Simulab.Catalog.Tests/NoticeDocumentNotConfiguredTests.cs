using System.Net;
using Simulab.Catalog.Contracts;
using Simulab.Storage;

namespace Simulab.Catalog.Tests;

/// <summary>
/// Open point 1 of F-89: a Development Api with no storage connection starts, an upload answers the stable
/// <c>storage.not_configured</c> code and keeps nothing, and reading a file finds none.
/// </summary>
public sealed class NoticeDocumentNotConfiguredTests : NoticeDocumentApiTests
{
    protected override bool UsesInMemoryFiles => false;

    [Fact]
    public async Task Upload_WithoutAStorageConnection_AnswersNotConfiguredAndKeepsNothing()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var response = await admin.SendAsync(Upload(NoticeOf(exam.Id, edition.Id), "edital.pdf", Pdf(100)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(StorageErrorCodes.NotConfigured);
        (await FindEditionAsync(admin, exam.Id, edition.Id)).NoticeDocument.Should().BeNull();
    }

    [Fact]
    public async Task Download_WithoutAStorageConnection_AnEditionWithoutAFileIsNotFound()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var response = await admin.GetAsync(NoticeOf(exam.Id, edition.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }
}
