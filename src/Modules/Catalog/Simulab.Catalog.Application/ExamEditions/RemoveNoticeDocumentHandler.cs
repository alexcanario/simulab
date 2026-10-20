using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Removes the notice PDF of an edition (F-89, UC2, BR2): the row first, then the blob, so a failed delete leaves an
/// orphan blob and never a row that points at nothing. An edition without a file is not an error: the answer is the same.
/// </summary>
public sealed class RemoveNoticeDocumentHandler(IExamEditionStore store, INoticeDocumentFiles files)
{
    public async Task<Result> HandleAsync(Guid examId, Guid id, CancellationToken cancellationToken = default)
    {
        var edition = await store.FindAsync(examId, id, cancellationToken);
        if (edition is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        var blobName = edition.RemoveNoticeDocument();
        if (blobName is null)
        {
            return Result.Success();
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure(refused);
        }

        await files.DeleteAsync(blobName, cancellationToken);

        return Result.Success();
    }
}
