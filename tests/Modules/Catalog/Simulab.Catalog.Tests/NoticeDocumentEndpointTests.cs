using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-89 through HTTP, as the Web calls it: upload, replace, remove and download of an edition's notice PDF, for the
/// curator and for the student. The files live in memory here; the blob emulator is proved by the storage block's tests.
/// </summary>
public sealed class NoticeDocumentEndpointTests : NoticeDocumentApiTests
{
    private InMemoryNoticeDocumentFiles _files => Files;

    // AC1 (UC1, BR1): the edition shows the file's name and size, and the download returns the same bytes.
    [Fact]
    public async Task Upload_AValidPdf_KeepsItOnTheEdition_AndTheDownloadReturnsTheSameBytes()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        var bytes = Pdf(5000);

        var uploaded = await UploadedAsync(admin, exam.Id, edition.Id, "Edital 01-2026.pdf", bytes);

        uploaded.NoticeDocument.Should().NotBeNull();
        uploaded.NoticeDocument!.FileName.Should().Be("Edital 01-2026.pdf");
        uploaded.NoticeDocument.Size.Should().Be(5000);
        (await FindEditionAsync(admin, exam.Id, edition.Id)).NoticeDocument.Should().Be(uploaded.NoticeDocument);
        (await admin.GetFromJsonAsync<List<ExamEditionResponse>>($"{Exams}/{exam.Id}/editions", AppJson.Options))!
            .Single().NoticeDocument.Should().Be(uploaded.NoticeDocument);

        using var download = await admin.GetAsync(NoticeOf(exam.Id, edition.Id));

        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        download.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        (download.Content.Headers.ContentDisposition.FileNameStar ?? download.Content.Headers.ContentDisposition.FileName)
            .Should().Be("Edital 01-2026.pdf");
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
    }

    // BR5: the JSON save of the form's fields never touches the file.
    [Fact]
    public async Task SavingTheFields_KeepsTheNoticeDocument()
    {
        var admin = await AdminAsync();
        var (exam, board, edition) = await SetUpAsync(admin);
        await UploadedAsync(admin, exam.Id, edition.Id, "edital.pdf", Pdf(100));

        var saved = await admin.PutAsJsonAsync(
            $"{Exams}/{exam.Id}/editions/{edition.Id}",
            Fields(board.Id, url: "https://www.exemplo.gov.br/edital.pdf"),
            AppJson.Options);

        saved.StatusCode.Should().Be(HttpStatusCode.OK, await saved.Content.ReadAsStringAsync());
        (await saved.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!.NoticeDocument!.FileName.Should().Be("edital.pdf");
    }

    // AC2 (BR1): neither the link nor the file is needed to save an edition as Published.
    [Fact]
    public async Task Save_PublishedWithNeitherLinkNorFile_IsSaved()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var saved = await CreateEditionAsync(admin, exam.Id, Fields(board.Id, nameof(ExamEditionStatus.Published)));

        saved.Status.Should().Be(ExamEditionStatus.Published);
        saved.NoticeUrl.Should().BeNull();
        saved.NoticeDocument.Should().BeNull();
        using var download = await admin.GetAsync(NoticeOf(exam.Id, saved.Id));
        download.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await download.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }

    // AC3 (UC2, BR2): the edition holds only the new file and the old blob is gone from the storage.
    [Fact]
    public async Task Upload_OverAnExistingFile_LeavesOnlyTheNewOneInTheStorage()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        await UploadedAsync(admin, exam.Id, edition.Id, "old.pdf", Pdf(100));
        var oldBlob = _files.Names.Should().ContainSingle().Subject;
        var replacement = Pdf(200);

        var replaced = await UploadedAsync(admin, exam.Id, edition.Id, "new.pdf", replacement);

        replaced.NoticeDocument!.FileName.Should().Be("new.pdf");
        replaced.NoticeDocument.Size.Should().Be(200);
        _files.Names.Should().ContainSingle().Which.Should().NotBe(oldBlob);
        using var download = await admin.GetAsync(NoticeOf(exam.Id, edition.Id));
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(replacement);
    }

    // AC4 (UC2, BR2): removing leaves the edition without a file and without a blob.
    [Fact]
    public async Task Remove_DeletesTheFileFromTheEditionAndTheStorage()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        await UploadedAsync(admin, exam.Id, edition.Id, "edital.pdf", Pdf(100));

        using var removed = await admin.DeleteAsync(NoticeOf(exam.Id, edition.Id));

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await FindEditionAsync(admin, exam.Id, edition.Id)).NoticeDocument.Should().BeNull();
        _files.Names.Should().BeEmpty();
        using var download = await admin.GetAsync(NoticeOf(exam.Id, edition.Id));
        download.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await download.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }

    [Fact]
    public async Task Remove_FromAnEditionWithoutAFile_IsNotAnError()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var removed = await admin.DeleteAsync(NoticeOf(exam.Id, edition.Id));

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task EveryRoute_OfAMissingEdition_AnswersEditionNotFound()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var missing = Guid.CreateVersion7();

        using var put = await admin.SendAsync(Upload(NoticeOf(exam.Id, missing), "a.pdf", Pdf(10)));
        using var delete = await admin.DeleteAsync(NoticeOf(exam.Id, missing));
        using var get = await admin.GetAsync(NoticeOf(exam.Id, missing));

        foreach (var response in new[] { put, delete, get })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNotFound);
        }
    }

    // AC5 (BR3): the name says pdf, the content does not: refused with the code, nothing stored, nothing changed.
    [Fact]
    public async Task Upload_AFileNamedPdfThatIsNotOne_IsRefusedWithNotPdfAndStoresNothing()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var response = await admin.SendAsync(Upload(NoticeOf(exam.Id, edition.Id), "notice.pdf", "<html>not a pdf</html>"u8.ToArray()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf);
        _files.Names.Should().BeEmpty();
        (await FindEditionAsync(admin, exam.Id, edition.Id)).NoticeDocument.Should().BeNull();
    }

    [Fact]
    public async Task Upload_AnEmptyFile_IsRefusedWithNotPdf()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var response = await admin.SendAsync(Upload(NoticeOf(exam.Id, edition.Id), "empty.pdf", []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf);
    }

    [Fact]
    public async Task Upload_WithoutAFilePart_IsRefusedWithNotPdf()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        using var request = new HttpRequestMessage(HttpMethod.Put, NoticeOf(exam.Id, edition.Id))
        {
            Content = new MultipartFormDataContent { { new StringContent("x"), "other" } }
        };

        using var response = await admin.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf);
    }

    // AC6 (BR4): exactly 20 MB is accepted; one byte more is refused before it is stored.
    [Fact]
    public async Task Upload_ExactlyTwentyMegabytes_IsAccepted()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        var uploaded = await UploadedAsync(admin, exam.Id, edition.Id, "big.pdf", Pdf(TwentyMegabytes));

        uploaded.NoticeDocument!.Size.Should().Be(TwentyMegabytes);
    }

    [Theory]
    [InlineData(TwentyMegabytes + 1)]
    [InlineData(TwentyMegabytes + (5L * 1024 * 1024))]
    public async Task Upload_OverTwentyMegabytes_IsRefusedWithTooLargeAndStoresNothing(long size)
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);

        using var response = await admin.SendAsync(Upload(NoticeOf(exam.Id, edition.Id), "big.pdf", Pdf(size)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentTooLarge);
        _files.Names.Should().BeEmpty();
        (await FindEditionAsync(admin, exam.Id, edition.Id)).NoticeDocument.Should().BeNull();
    }

    // AC9 (BR6): without catalog.manage the three back-office routes are closed.
    [Fact]
    public async Task BackOfficeRoutes_WithoutTheManagePermission_AreForbidden()
    {
        var student = await StudentAsync();
        var exam = Guid.CreateVersion7();
        var edition = Guid.CreateVersion7();

        using var put = await student.SendAsync(Upload(NoticeOf(exam, edition), "a.pdf", Pdf(10)));
        using var delete = await student.DeleteAsync(NoticeOf(exam, edition));
        using var get = await student.GetAsync(NoticeOf(exam, edition));

        foreach (var response in new[] { put, delete, get })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    // AC10 (UC4, BR6): a student gets the PDF of a published edition.
    [Fact]
    public async Task Student_DownloadsTheFileOfAPublishedEdition()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin, nameof(ExamEditionStatus.Published));
        var bytes = Pdf(3000);
        await UploadedAsync(admin, exam.Id, edition.Id, "edital.pdf", bytes);
        var student = await StudentAsync();

        using var response = await student.GetAsync(StudentNoticeOf(exam.Id, edition.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
    }

    // AC10 (BR6): a draft edition's file answers as not found, even beside a published edition of the same exam.
    [Fact]
    public async Task Student_AskingForTheFileOfADraftEdition_GetsNotFound()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        await CreateEditionAsync(admin, exam.Id, Fields(board.Id, nameof(ExamEditionStatus.Published), year: ThisYear - 1));
        var draft = await CreateEditionAsync(admin, exam.Id, Fields(board.Id));
        await UploadedAsync(admin, exam.Id, draft.Id, "draft.pdf", Pdf(100));
        var student = await StudentAsync();

        using var response = await student.GetAsync(StudentNoticeOf(exam.Id, draft.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }

    [Fact]
    public async Task Student_AskingForAPublishedEditionWithoutAFile_GetsNotFound()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin, nameof(ExamEditionStatus.Published));
        var student = await StudentAsync();

        using var response = await student.GetAsync(StudentNoticeOf(exam.Id, edition.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }

    [Fact]
    public async Task Student_AskingForAMissingEdition_GetsNotFound()
    {
        var student = await StudentAsync();

        using var response = await student.GetAsync(StudentNoticeOf(Guid.CreateVersion7(), Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
    }

    // AC11 (API half): the published exam lists the file of the edition that has one, and null for the one that has not.
    [Fact]
    public async Task PublishedExam_ListsTheNoticeDocumentOnlyOnTheEditionThatHasOne()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var withFile = await CreateEditionAsync(admin, exam.Id, Fields(board.Id, nameof(ExamEditionStatus.Published), year: ThisYear));
        var without = await CreateEditionAsync(admin, exam.Id, Fields(board.Id, nameof(ExamEditionStatus.Published), year: ThisYear - 1));
        await UploadedAsync(admin, exam.Id, withFile.Id, "edital.pdf", Pdf(2048));
        var student = await StudentAsync();

        var detail = await student.GetFromJsonAsync<PublishedExamDetailResponse>($"{Published}/{exam.Id}", AppJson.Options);

        var byId = detail!.Editions.ToDictionary(row => row.Id);
        byId[withFile.Id].NoticeDocument.Should().NotBeNull();
        byId[withFile.Id].NoticeDocument!.FileName.Should().Be("edital.pdf");
        byId[withFile.Id].NoticeDocument!.Size.Should().Be(2048);
        byId[without.Id].NoticeDocument.Should().BeNull();
    }

    // AC12 (BR7): deleting the edition deletes its file from the storage.
    [Fact]
    public async Task DeletingTheEdition_DeletesItsFileFromTheStorage()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        await UploadedAsync(admin, exam.Id, edition.Id, "edital.pdf", Pdf(100));
        _files.Names.Should().ContainSingle();

        using var deleted = await admin.DeleteAsync($"{Exams}/{exam.Id}/editions/{edition.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _files.Names.Should().BeEmpty();
    }

    // AC13 (BR8): no answer carries the name the file is stored under, nor an address of the storage.
    [Fact]
    public async Task Responses_NeverCarryTheBlobNameOrAStorageAddress()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin, nameof(ExamEditionStatus.Published));
        using var upload = await admin.SendAsync(Upload(NoticeOf(exam.Id, edition.Id), "edital.pdf", Pdf(100)));
        var blobName = _files.Names.Should().ContainSingle().Subject;
        var student = await StudentAsync();

        var bodies = new[]
        {
            await upload.Content.ReadAsStringAsync(),
            await (await admin.GetAsync($"{Exams}/{exam.Id}/editions/{edition.Id}")).Content.ReadAsStringAsync(),
            await (await admin.GetAsync($"{Exams}/{exam.Id}/editions")).Content.ReadAsStringAsync(),
            await (await student.GetAsync($"{Published}/{exam.Id}")).Content.ReadAsStringAsync()
        };

        foreach (var body in bodies)
        {
            body.Should().NotContain(blobName).And.NotContain("exam-editions/").And.NotContain("blob.core").And.NotContain("127.0.0.1");
        }

        blobName.Should().StartWith($"exam-editions/{edition.Id}/notice/").And.EndWith(".pdf");
        blobName.Should().HaveLength(CatalogLimits.ExamEditionNoticeDocumentBlobNameLength);
    }
}
