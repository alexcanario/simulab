using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// Adds a notice subject to an edition or replaces the fields of an existing one (F-74, UC2 and UC3). The
/// edition must exist under the exam and the label must be free inside its group; the unique index is the
/// real guarantee. A new row goes last in its group, and so does a row whose group changed (BR6, BR7); an
/// edit that keeps the group keeps the position (AC9).
/// </summary>
public sealed class SaveNoticeSubjectHandler(INoticeSubjectStore store, IExamEditionStore editions)
{
    /// <param name="examId">The exam of the route.</param>
    /// <param name="editionId">The edition of the route.</param>
    /// <param name="id">The notice subject to update, or null to create one.</param>
    /// <param name="request">The group, the label and the number of questions.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public async Task<Result<NoticeSubjectResponse>> HandleAsync(
        Guid examId,
        Guid editionId,
        Guid? id,
        SaveNoticeSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await editions.FindAsync(examId, editionId, cancellationToken) is null)
        {
            return Result.Failure<NoticeSubjectResponse>(
                new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        var existing = id is { } existingId ? await store.FindAsync(editionId, existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<NoticeSubjectResponse>(
                new Error(CatalogErrorCodes.NoticeSubjectNotFound, ErrorKind.NotFound));
        }

        var candidate = NoticeSubject.Create(editionId, request.Group, request.Label, request.QuestionCount);
        if (candidate.IsFailure)
        {
            return Result.Failure<NoticeSubjectResponse>(candidate.Error!);
        }

        if (await store.IsDuplicateAsync(
                editionId,
                candidate.Value.NormalizedGroup,
                candidate.Value.NormalizedLabel,
                id,
                cancellationToken))
        {
            return Result.Failure<NoticeSubjectResponse>(
                new Error(CatalogErrorCodes.NoticeSubjectDuplicate, ErrorKind.Conflict));
        }

        var subject = existing ?? candidate.Value;
        var groupChanged = existing is null || existing.NormalizedGroup != candidate.Value.NormalizedGroup;
        if (existing is null)
        {
            store.Add(subject);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            subject.Update(request.Group, request.Label, request.QuestionCount);
        }

        if (groupChanged)
        {
            var siblings = await store.ListForEditionAsync(editionId, cancellationToken);
            NoticeSubjectOrder.PlaceLast(siblings, subject);
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<NoticeSubjectResponse>(refused);
        }

        return Result.Success(
            new NoticeSubjectResponse(subject.Id, subject.ExamEditionId, subject.Group, subject.Label, subject.QuestionCount));
    }
}
