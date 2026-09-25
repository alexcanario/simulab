using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Exams;

/// <summary>
/// Removes an exam from the catalog (UC4). The removal is a soft delete (BR1): the row stays, so its name
/// stays taken inside its issuing authority (BR10). Nothing points at an exam yet — the guard for an exam
/// that has editions arrives with F-35 (BR13).
/// </summary>
public sealed class DeleteExamHandler(IExamStore store)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exam = await store.FindAsync(id, cancellationToken);
        if (exam is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamNotFound, ErrorKind.NotFound));
        }

        store.Remove(exam);
        await store.TrySaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
