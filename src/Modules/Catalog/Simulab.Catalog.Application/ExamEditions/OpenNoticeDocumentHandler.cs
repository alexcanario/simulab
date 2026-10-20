using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Opens an edition's notice PDF for download (F-89, UC3 and UC4, BR6). The curator reads it from any edition, and a
/// missing edition is told apart from a missing file. A student reads it only from a published edition, and every other
/// case — a draft, a missing edition, an edition without a file — answers the same "not found", so a draft's file is not
/// confirmed to exist.
/// </summary>
public sealed class OpenNoticeDocumentHandler(IExamEditionStore store, INoticeDocumentFiles files)
{
    public async Task<Result<NoticeDocumentDownload>> HandleAsync(
        Guid examId,
        Guid id,
        bool publishedOnly,
        CancellationToken cancellationToken = default)
    {
        var edition = await store.FindAsync(examId, id, cancellationToken);
        if (edition is null && !publishedOnly)
        {
            return Failure(CatalogErrorCodes.ExamEditionNotFound);
        }

        if (edition?.NoticeDocument is not { } document || (publishedOnly && edition.Status != ExamEditionStatus.Published))
        {
            return Failure(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound);
        }

        var content = await files.OpenReadAsync(document.BlobName, cancellationToken);

        return content is null
            ? Failure(CatalogErrorCodes.ExamEditionNoticeDocumentNotFound)
            : Result.Success(new NoticeDocumentDownload(content, document.FileName, document.Size));
    }

    private static Result<NoticeDocumentDownload> Failure(string code) =>
        Result.Failure<NoticeDocumentDownload>(new Error(code, ErrorKind.NotFound));
}
