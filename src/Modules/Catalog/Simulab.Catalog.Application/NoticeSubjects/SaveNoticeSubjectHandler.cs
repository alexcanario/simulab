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
public sealed class SaveNoticeSubjectHandler(
    INoticeSubjectStore store,
    IExamEditionStore editions,
    INoticeSubjectQueries queries)
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

        // The mapping is checked before anything is added or changed, so a refusal leaves the row as it was (AC2).
        var requested = NoticeSubjectMappingRules.Normalize(request.Mappings);
        if (requested.IsFailure)
        {
            return Result.Failure<NoticeSubjectResponse>(requested.Error!);
        }

        var saved = existing is null ? [] : await store.ListMappingsAsync(existing.Id, cancellationToken);
        var refusal = await CheckMappingAsync(requested.Value, saved, cancellationToken);
        if (refusal is not null)
        {
            return Result.Failure<NoticeSubjectResponse>(refusal);
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

        ReplaceMapping(subject.Id, saved, requested.Value);

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

        // The answer carries the names of the mapped items, which the entity does not hold.
        var answer = await queries.FindAsync(editionId, subject.Id, cancellationToken);

        return Result.Success(
            answer ?? new NoticeSubjectResponse(subject.Id, subject.ExamEditionId, subject.Group, subject.Label, subject.QuestionCount));
    }

    /// <summary>BR3 and BR4: every target exists and is live, and no topic joins its own whole subject.</summary>
    private async Task<Error?> CheckMappingAsync(
        IReadOnlyList<MappingEntry> requested,
        IReadOnlyList<NoticeSubjectMapping> saved,
        CancellationToken cancellationToken)
    {
        if (requested.Count == 0)
        {
            return null;
        }

        var subjectIds = requested.Where(entry => entry.SubjectId is not null).Select(entry => entry.SubjectId!.Value).ToList();
        var topicIds = requested.Where(entry => entry.TopicId is not null).Select(entry => entry.TopicId!.Value).ToList();
        var targets = await store.FindLiveTargetsAsync(subjectIds, topicIds, cancellationToken);

        if (subjectIds.Any(id => !targets.SubjectIds.Contains(id)) || topicIds.Any(id => !targets.TopicSubjects.ContainsKey(id)))
        {
            return new Error(CatalogErrorCodes.NoticeSubjectMappingTargetNotFound, ErrorKind.Validation);
        }

        var savedEntries = saved.Select(row => new MappingEntry(row.SubjectId, row.TopicId)).ToList();
        var overlap = NoticeSubjectMappingRules.CheckOverlap(requested, targets.TopicSubjects, savedEntries);

        return overlap.IsFailure ? overlap.Error : null;
    }

    /// <summary>BR7: the save replaces the mapping. Dropped entries go, kept ones stay, new ones are added.</summary>
    private void ReplaceMapping(
        Guid noticeSubjectId,
        IReadOnlyList<NoticeSubjectMapping> saved,
        IReadOnlyList<MappingEntry> requested)
    {
        var wanted = requested.ToHashSet();
        var kept = new HashSet<MappingEntry>();
        foreach (var row in saved)
        {
            var entry = new MappingEntry(row.SubjectId, row.TopicId);
            if (wanted.Contains(entry) && kept.Add(entry))
            {
                continue;
            }

            store.RemoveMapping(row);
        }

        foreach (var entry in requested.Where(entry => !kept.Contains(entry)))
        {
            store.AddMapping(entry.SubjectId is { } subjectId
                ? NoticeSubjectMapping.ForSubject(noticeSubjectId, subjectId)
                : NoticeSubjectMapping.ForTopic(noticeSubjectId, entry.TopicId!.Value));
        }
    }
}
