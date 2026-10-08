using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Removes an edition from its exam (UC4 and UC5). The removal is a soft delete (BR1): the row stays, so its
/// year, position and board stay taken inside the exam (BR10). A published edition does not leave at all —
/// a student may be looking at it — until it is set back to Draft (BR11). The edition's notice subjects leave
/// with it, in the same save (F-74, BR10): both stores share the one scoped context, so it is one
/// <c>SaveChanges</c> and one transaction.
/// </summary>
public sealed class DeleteExamEditionHandler(IExamEditionStore store, INoticeSubjectStore noticeSubjects)
{
    public async Task<Result> HandleAsync(Guid examId, Guid id, CancellationToken cancellationToken = default)
    {
        var edition = await store.FindAsync(examId, id, cancellationToken);
        if (edition is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        var allowed = edition.CanBeDeleted();
        if (allowed.IsFailure)
        {
            return allowed;
        }

        foreach (var subject in await noticeSubjects.ListForEditionAsync(edition.Id, cancellationToken))
        {
            noticeSubjects.Remove(subject);
        }

        store.Remove(edition);
        var refused = await store.TrySaveChangesAsync(cancellationToken);

        return refused is null ? Result.Success() : Result.Failure(refused);
    }
}
