using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-89 data: the four notice document columns are all set or all empty, and the size stays inside the limit. The
/// database says it too, in case anything bypasses the entity (a half row would leave a download with no name).
/// </summary>
public sealed class NoticeDocumentColumnsTests : NoticeDocumentApiTests
{
    private const string CheckViolation = "23514";

    private async Task<Guid> EditionWithAFileAsync()
    {
        var admin = await AdminAsync();
        var (exam, _, edition) = await SetUpAsync(admin);
        await UploadedAsync(admin, exam.Id, edition.Id, "edital.pdf", Pdf(100));

        return edition.Id;
    }

    [Fact]
    public async Task AFileWithoutItsName_IsRefusedByTheDatabase()
    {
        var edition = await EditionWithAFileAsync();

        var act = () => QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE catalog.exam_editions SET notice_document_file_name = NULL WHERE id = {edition}"));

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(CheckViolation);
    }

    [Fact]
    public async Task AFileOver20Megabytes_IsRefusedByTheDatabase()
    {
        var edition = await EditionWithAFileAsync();
        var tooBig = CatalogLimits.ExamEditionNoticeDocumentMaxBytes + 1;

        var act = () => QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE catalog.exam_editions SET notice_document_size = {tooBig} WHERE id = {edition}"));

        (await act.Should().ThrowAsync<Exception>()).Which.ToString().Should().Contain(CheckViolation);
    }

    [Fact]
    public async Task AnEditionWithoutAFile_HasAllFourColumnsEmpty()
    {
        var admin = await AdminAsync();
        var (_, _, edition) = await SetUpAsync(admin);

        var filled = await QueryAsync(context => context.Database.SqlQuery<int>(
            $"SELECT count(*)::int AS \"Value\" FROM catalog.exam_editions WHERE id = {edition.Id} AND (notice_document_blob_name IS NOT NULL OR notice_document_file_name IS NOT NULL OR notice_document_size IS NOT NULL OR notice_document_uploaded_at IS NOT NULL)")
            .SingleAsync());

        filled.Should().Be(0);
    }
}
