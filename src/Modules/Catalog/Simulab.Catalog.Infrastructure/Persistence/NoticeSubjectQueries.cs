using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// An edition's notice subjects in display order (F-74, UC1), all in one call, each with the canonical subjects
/// and topics it covers (F-75, UC1). The mapping is read in two more queries for the whole list, never one per
/// row: whole-subject entries join the subject, topic entries join the topic and its current subject (BR8).
/// </summary>
public sealed class NoticeSubjectQueries(CatalogModuleDbContext context) : INoticeSubjectQueries
{
    public async Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(
        Guid examEditionId,
        CancellationToken cancellationToken) =>
        await WithMappingsAsync(
            context.NoticeSubjects.Where(subject => subject.ExamEditionId == examEditionId),
            cancellationToken);

    public async Task<NoticeSubjectResponse?> FindAsync(
        Guid examEditionId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var rows = await WithMappingsAsync(
            context.NoticeSubjects.Where(subject => subject.ExamEditionId == examEditionId && subject.Id == id),
            cancellationToken);

        return rows.Count == 0 ? null : rows[0];
    }

    // The soft-delete filters leave out deleted editions and deleted rows. The order is the editions section's
    // (F-35 BR14): normalized, so an accented name sorts with its letter.
    public async Task<IReadOnlyList<NoticeSubjectCopySourceResponse>> ListCopySourcesAsync(
        Guid examId,
        Guid targetEditionId,
        CancellationToken cancellationToken) =>
        await (
                from edition in context.ExamEditions.AsNoTracking()
                where edition.ExamId == examId && edition.Id != targetEditionId
                join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
                let count = context.NoticeSubjects.Count(subject => subject.ExamEditionId == edition.Id)
                where count > 0
                orderby edition.NoticeYear descending, edition.NormalizedPosition, organizer.NormalizedName
                select new NoticeSubjectCopySourceResponse(edition.Id, edition.NoticeYear, edition.Position, organizer.Name, count))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The notice subjects of several editions at once, each edition's list in display order (F-76): the same fixed
    /// number of queries whatever the number of editions or rows. An edition with none has no key.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<NoticeSubjectResponse>>> ListByEditionsAsync(
        IReadOnlyCollection<Guid> examEditionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(examEditionIds);

        var rows = await WithMappingsAsync(
            context.NoticeSubjects.Where(subject => examEditionIds.Contains(subject.ExamEditionId)),
            cancellationToken);

        return rows
            .GroupBy(row => row.ExamEditionId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<NoticeSubjectResponse>)[.. group]);
    }

    private async Task<IReadOnlyList<NoticeSubjectResponse>> WithMappingsAsync(
        IQueryable<Domain.Entities.NoticeSubject> subjects,
        CancellationToken cancellationToken)
    {
        var rows = await subjects
            .AsNoTracking()
            .OrderBy(subject => subject.DisplayOrder)
            .ThenBy(subject => subject.CreatedAt)
            .ThenBy(subject => subject.Id)
            .Select(subject => new
            {
                subject.Id,
                subject.ExamEditionId,
                subject.Group,
                subject.Label,
                subject.QuestionCount,
                subject.PointsPerQuestion
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(row => row.Id).ToList();

        var wholeSubjects = await (
                from mapping in context.NoticeSubjectMappings.AsNoTracking()
                join subject in context.Subjects on mapping.SubjectId equals subject.Id
                where ids.Contains(mapping.NoticeSubjectId)
                select new { mapping.NoticeSubjectId, Entry = new NoticeSubjectMappingResponse(subject.Id, subject.Name, null, null) })
            .ToListAsync(cancellationToken);

        var topics = await (
                from mapping in context.NoticeSubjectMappings.AsNoTracking()
                join topic in context.Topics on mapping.TopicId equals topic.Id
                join subject in context.Subjects on topic.SubjectId equals subject.Id
                where ids.Contains(mapping.NoticeSubjectId)
                select new { mapping.NoticeSubjectId, Entry = new NoticeSubjectMappingResponse(subject.Id, subject.Name, topic.Id, topic.Name) })
            .ToListAsync(cancellationToken);

        var byRow = wholeSubjects.Concat(topics).ToLookup(item => item.NoticeSubjectId, item => item.Entry);

        return rows
            .Select(row => new NoticeSubjectResponse(
                row.Id,
                row.ExamEditionId,
                row.Group,
                row.Label,
                row.QuestionCount,
                byRow[row.Id].ToList(),
                row.PointsPerQuestion))
            .ToList();
    }
}
