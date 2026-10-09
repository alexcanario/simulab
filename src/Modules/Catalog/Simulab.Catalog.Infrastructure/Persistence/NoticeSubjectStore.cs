using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The notice subject port over the module's context (F-74). Every read keeps the soft-delete filter: the
/// unique index is filtered the same way, so a deleted row neither shows nor holds its label (BR5).
/// </summary>
public sealed class NoticeSubjectStore(CatalogModuleDbContext context) : INoticeSubjectStore
{
    public Task<NoticeSubject?> FindAsync(Guid examEditionId, Guid id, CancellationToken cancellationToken) =>
        context.NoticeSubjects.FirstOrDefaultAsync(
            subject => subject.Id == id && subject.ExamEditionId == examEditionId,
            cancellationToken);

    public async Task<IReadOnlyList<NoticeSubject>> ListForEditionAsync(
        Guid examEditionId,
        CancellationToken cancellationToken) =>
        await context.NoticeSubjects
            .Where(subject => subject.ExamEditionId == examEditionId)
            .OrderBy(subject => subject.DisplayOrder)
            .ThenBy(subject => subject.CreatedAt)
            .ThenBy(subject => subject.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> IsDuplicateAsync(
        Guid examEditionId,
        string normalizedGroup,
        string normalizedLabel,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var query = context.NoticeSubjects.Where(subject =>
            subject.ExamEditionId == examEditionId
            && subject.NormalizedGroup == normalizedGroup
            && subject.NormalizedLabel == normalizedLabel);

        // The exclusion is its own Where, not `Id != exceptId`: a null parameter in a comparison is SQL's
        // three-valued logic (the F-33 lesson).
        return (exceptId is { } id ? query.Where(subject => subject.Id != id) : query).AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoticeSubjectMapping>> ListMappingsAsync(
        Guid noticeSubjectId,
        CancellationToken cancellationToken) =>
        await context.NoticeSubjectMappings
            .Where(mapping => mapping.NoticeSubjectId == noticeSubjectId)
            .ToListAsync(cancellationToken);

    public async Task<MappingTargets> FindLiveTargetsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken)
    {
        var subjects = await context.Subjects
            .AsNoTracking()
            .Where(subject => subjectIds.Contains(subject.Id))
            .Select(subject => subject.Id)
            .ToListAsync(cancellationToken);

        var topics = await context.Topics
            .AsNoTracking()
            .Where(topic => topicIds.Contains(topic.Id))
            .Select(topic => new { topic.Id, topic.SubjectId })
            .ToListAsync(cancellationToken);

        return new MappingTargets(subjects.ToHashSet(), topics.ToDictionary(topic => topic.Id, topic => topic.SubjectId));
    }

    public void AddMapping(NoticeSubjectMapping mapping) => context.NoticeSubjectMappings.Add(mapping);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE.
    public void RemoveMapping(NoticeSubjectMapping mapping) => context.NoticeSubjectMappings.Remove(mapping);

    public void Add(NoticeSubject subject) => context.NoticeSubjects.Add(subject);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE (BR10).
    public void Remove(NoticeSubject subject) => context.NoticeSubjects.Remove(subject);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (NoticeSubjectUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();

            return NoticeSubjectUniqueViolations.Translate(exception);
        }
    }
}
