using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// Removes a notice subject from its edition (F-74, UC5, BR10): a soft delete, so the same label can be added
/// to the group again (AC8). The remaining rows are not renumbered: a gap is harmless, moves are relative.
/// </summary>
public sealed class DeleteNoticeSubjectHandler(INoticeSubjectStore store, IExamEditionStore editions)
{
    public async Task<Result> HandleAsync(
        Guid examId,
        Guid editionId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await editions.FindAsync(examId, editionId, cancellationToken) is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        var subject = await store.FindAsync(editionId, id, cancellationToken);
        if (subject is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectNotFound, ErrorKind.NotFound));
        }

        store.Remove(subject);
        var refused = await store.TrySaveChangesAsync(cancellationToken);

        return refused is null ? Result.Success() : Result.Failure(refused);
    }
}
