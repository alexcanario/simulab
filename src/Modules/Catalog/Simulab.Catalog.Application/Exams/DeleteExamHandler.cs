using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Exams;

/// <summary>
/// Removes an exam from the catalog (UC4). The removal is a soft delete (BR1): the row stays, so its name
/// stays taken inside its issuing authority (BR10). An exam that still has editions does not leave at all
/// (F-35 BR12): cascading would let one click remove a whole history.
/// </summary>
public sealed class DeleteExamHandler(IExamStore store, IExamEditionStore editions)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exam = await store.FindAsync(id, cancellationToken);
        if (exam is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamNotFound, ErrorKind.NotFound));
        }

        // Deleted editions do not count: they are gone from the catalog, so nothing is orphaned.
        if (await editions.ExamHasEditionsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamHasEditions, ErrorKind.Conflict));
        }

        store.Remove(exam);
        await store.TrySaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
