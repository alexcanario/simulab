using Microsoft.Extensions.Logging;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.SharedKernel.Results;
using Simulab.Storage;

namespace Simulab.Catalog.Infrastructure.Files;

/// <summary>
/// <see cref="INoticeDocumentFiles"/> over the storage building block (F-89): the notice PDFs live in the one private
/// container, under the names the handlers make up. A delete that fails is logged and left as an orphan blob — the row
/// has already stopped pointing at it, and a lost file is worse than a stray one (decisions of 2026-10-04).
/// </summary>
public sealed class NoticeDocumentFiles(IFileStorage storage, ILogger<NoticeDocumentFiles> logger) : INoticeDocumentFiles
{
    private const string PdfContentType = "application/pdf";

    public Task<Result> SaveAsync(string blobName, Stream content, CancellationToken cancellationToken = default) =>
        storage.SaveAsync(blobName, content, PdfContentType, cancellationToken);

    public Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
        storage.OpenReadAsync(blobName, cancellationToken);

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            await storage.DeleteAsync(blobName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The notice PDF {BlobName} could not be deleted and stays in the storage as an orphan.", blobName);
        }
    }
}
