using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// Moves a notice subject one place up or down inside its group (F-74, UC4, BR6). The server, not the screen,
/// knows the neighbours, and two rows change in one save.
/// </summary>
public sealed class MoveNoticeSubjectHandler(INoticeSubjectStore store, IExamEditionStore editions)
{
    public async Task<Result> HandleAsync(
        Guid examId,
        Guid editionId,
        Guid id,
        MoveNoticeSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await editions.FindAsync(examId, editionId, cancellationToken) is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        var subject = await store.FindAsync(editionId, id, cancellationToken);
        if (subject is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectNotFound, ErrorKind.NotFound));
        }

        if (request.ParseDirection() is not { } direction)
        {
            return Result.Failure(new Error(CatalogErrorCodes.NoticeSubjectMoveInvalid, ErrorKind.Validation));
        }

        var siblings = await store.ListForEditionAsync(editionId, cancellationToken);
        // Both reads go through the same context, so the row found above is the instance in the list.
        var moved = NoticeSubjectOrder.Move(siblings, subject, direction);
        if (moved.IsFailure)
        {
            return moved;
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);

        return refused is null ? Result.Success() : Result.Failure(refused);
    }
}
