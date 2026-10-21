using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Removes an edition from its exam (UC4 and UC5). The removal is a soft delete (BR1): the row stays, so its
/// year, position and board stay taken inside the exam (BR10). A published edition does not leave at all —
/// a student may be looking at it — until it is set back to Draft (BR11). The edition's notice subjects leave
/// with it, in the same save (F-74, BR10): both stores share the one scoped context, so it is one
/// <c>SaveChanges</c> and one transaction. So does its notice PDF (F-89, BR7): the row stops pointing at it in that save,
/// and the file is deleted from the storage once the save is done.
/// </summary>
public sealed class DeleteExamEditionHandler(
    IExamEditionStore store,
    INoticeSubjectStore noticeSubjects,
    INoticeDocumentFiles files,
    IEnumerable<IExamEditionUsageCheck> usageChecks)
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

        // F-96 BR8: a live question of any status points at this edition, so removing it would orphan its source.
        if (await UsageChecks.AnyAsync(
                usageChecks, edition.Id, (check, ids, token) => check.InUseAsync(ids, token), cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionHasQuestions, ErrorKind.Conflict));
        }

        foreach (var subject in await noticeSubjects.ListForEditionAsync(edition.Id, cancellationToken))
        {
            noticeSubjects.Remove(subject);
        }

        var blobName = edition.RemoveNoticeDocument();
        store.Remove(edition);
        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure(refused);
        }

        if (blobName is not null)
        {
            await files.DeleteAsync(blobName, cancellationToken);
        }

        return Result.Success();
    }
}
