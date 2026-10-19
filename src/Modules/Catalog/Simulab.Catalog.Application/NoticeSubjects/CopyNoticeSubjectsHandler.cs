using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// Copies the live notice subjects of another edition of the same exam into an edition that has none (F-86, UC1):
/// group, label, number of questions, position and the F-75 mapping, as the source holds them now (BR3, BR6). The
/// copies are new rows (BR4) written in one save, so either every row and its mapping is written or none is (BR5).
/// The checks run source first, then target, then content: a source that cannot be used is 404 whatever the target
/// holds (BR7).
/// </summary>
public sealed class CopyNoticeSubjectsHandler(INoticeSubjectStore store, IExamEditionStore editions)
{
    /// <param name="examId">The exam of the route.</param>
    /// <param name="targetEditionId">The edition that receives the copies.</param>
    /// <param name="request">The source edition.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public async Task<Result<CopyNoticeSubjectsResponse>> HandleAsync(
        Guid examId,
        Guid targetEditionId,
        CopyNoticeSubjectsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await editions.FindAsync(examId, targetEditionId, cancellationToken) is null)
        {
            return Failure(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound);
        }

        // BR1, BR7: the source is another live edition of this exam; the target itself is never a source.
        if (request.SourceEditionId is not { } sourceId
            || sourceId == targetEditionId
            || await editions.FindAsync(examId, sourceId, cancellationToken) is null)
        {
            return Failure(CatalogErrorCodes.ExamEditionCopySourceNotFound, ErrorKind.NotFound);
        }

        // BR2: only an empty list takes a copy; deleted rows are not in the answer.
        if ((await store.ListForEditionAsync(targetEditionId, cancellationToken)).Count > 0)
        {
            return Failure(CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty, ErrorKind.Conflict);
        }

        var sourceRows = await store.ListForEditionAsync(sourceId, cancellationToken);
        if (sourceRows.Count == 0)
        {
            return Failure(CatalogErrorCodes.NoticeSubjectCopySourceEmpty, ErrorKind.Validation);
        }

        var sourceMappings = (await store.ListMappingsOfEditionAsync(sourceId, cancellationToken))
            .ToLookup(mapping => mapping.NoticeSubjectId);

        // The source is already in display order with each group together; numbering from 1 keeps that order.
        var position = 1;
        foreach (var row in sourceRows)
        {
            var copy = row.CopyTo(targetEditionId, position++);
            store.Add(copy);

            // BR6: entry by entry, as the source holds it; an overlap the source holds is copied as is.
            foreach (var mapping in sourceMappings[row.Id])
            {
                store.AddMapping(mapping.SubjectId is { } subjectId
                    ? NoticeSubjectMapping.ForSubject(copy.Id, subjectId)
                    : NoticeSubjectMapping.ForTopic(copy.Id, mapping.TopicId!.Value));
            }
        }

        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is null)
        {
            return Result.Success(new CopyNoticeSubjectsResponse(sourceRows.Count));
        }

        // The unique index refused a row after the check passed: another admin wrote to the target in between.
        return refused.Code == CatalogErrorCodes.NoticeSubjectDuplicate
            ? Failure(CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty, ErrorKind.Conflict)
            : Result.Failure<CopyNoticeSubjectsResponse>(refused);
    }

    private static Result<CopyNoticeSubjectsResponse> Failure(string code, ErrorKind kind) =>
        Result.Failure<CopyNoticeSubjectsResponse>(new Error(code, kind));
}
