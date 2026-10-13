using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// What another module may ask about topics (F-95): a live topic with the subject it sits under. The two
/// global soft-delete filters leave out a deleted topic and a topic whose subject was deleted.
/// </summary>
public sealed class TopicDirectory(CatalogModuleDbContext context) : ITopicDirectory
{
    public async Task<TopicDirectoryEntry?> FindAsync(Guid topicId, CancellationToken cancellationToken)
    {
        var found = await FindManyAsync([topicId], cancellationToken);

        return found.Count > 0 ? found[0] : null;
    }

    public async Task<IReadOnlyList<TopicDirectoryEntry>> FindManyAsync(
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(topicIds);

        if (topicIds.Count == 0)
        {
            return [];
        }

        var ids = topicIds.ToList();

        return await context.Topics
            .AsNoTracking()
            .Where(topic => ids.Contains(topic.Id))
            .Join(
                context.Subjects,
                topic => topic.SubjectId,
                subject => subject.Id,
                (topic, subject) => new TopicDirectoryEntry(topic.Id, topic.Name, subject.Id, subject.Name))
            .ToListAsync(cancellationToken);
    }
}
