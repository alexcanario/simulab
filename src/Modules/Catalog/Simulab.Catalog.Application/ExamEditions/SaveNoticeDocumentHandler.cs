using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Keeps a PDF as the notice of an edition, replacing the one it had (F-89, UC1 and UC2). The file is checked before
/// anything is stored: at most 20 MB (BR4) and a content that starts with <c>%PDF-</c> (BR3), whatever its name says.
/// The order protects the file (decisions of 2026-10-04): the new blob is written first, then the row, and only then the
/// old blob is deleted, so the row never points at a missing file; a refused save removes the new blob again.
/// </summary>
public sealed class SaveNoticeDocumentHandler(
    IExamEditionStore store,
    IExamEditionQueries queries,
    INoticeDocumentFiles files,
    TimeProvider clock)
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    // content must be a stream that can seek: its first bytes are read, then it is read again from the start.
    public async Task<Result<ExamEditionResponse>> HandleAsync(
        Guid examId,
        Guid id,
        string fileName,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var edition = await store.FindAsync(examId, id, cancellationToken);
        if (edition is null)
        {
            return Failure(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound);
        }

        if (length > CatalogLimits.ExamEditionNoticeDocumentMaxBytes)
        {
            return Failure(CatalogErrorCodes.ExamEditionNoticeDocumentTooLarge, ErrorKind.Validation);
        }

        if (!await StartsWithPdfSignatureAsync(content, cancellationToken))
        {
            return Failure(CatalogErrorCodes.ExamEditionNoticeDocumentNotPdf, ErrorKind.Validation);
        }

        var blobName = $"exam-editions/{edition.Id}/notice/{Guid.CreateVersion7()}.pdf";
        var stored = await files.SaveAsync(blobName, content, cancellationToken);
        if (stored.IsFailure)
        {
            return Result.Failure<ExamEditionResponse>(stored.Error!);
        }

        var attached = edition.AttachNoticeDocument(blobName, fileName, length, clock.GetUtcNow());
        var refused = attached.IsFailure ? attached.Error : await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            await files.DeleteAsync(blobName, cancellationToken);
            return Result.Failure<ExamEditionResponse>(refused);
        }

        if (attached.Value is { } replacedBlob)
        {
            await files.DeleteAsync(replacedBlob, cancellationToken);
        }

        var saved = await queries.FindAsync(examId, id, cancellationToken);

        return saved is null
            ? Failure(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound)
            : Result.Success(saved);
    }

    private static async Task<bool> StartsWithPdfSignatureAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[PdfSignature.Length];
        content.Position = 0;
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;

        return read == header.Length && header.AsSpan().SequenceEqual(PdfSignature);
    }

    private static Result<ExamEditionResponse> Failure(string code, ErrorKind kind) =>
        Result.Failure<ExamEditionResponse>(new Error(code, kind));
}
