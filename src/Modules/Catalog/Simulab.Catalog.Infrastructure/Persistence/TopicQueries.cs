using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// A subject's topics (F-79, UC5): all in one call, sorted over the normalized name so "Árvore" sorts with
/// the A's and not after Z (the F-33 lesson). A topic is in use when a live notice subject maps it (F-75) or
/// when any module's <see cref="ITopicUsageCheck"/> says so (F-95): each check is asked once for the whole list.
/// </summary>
public sealed class TopicQueries(CatalogModuleDbContext context, IEnumerable<ITopicUsageCheck> usageChecks)
    : ITopicQueries
{
    public async Task<IReadOnlyList<TopicResponse>> ListAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        var topics = await context.Topics
            .AsNoTracking()
            .Where(topic => topic.SubjectId == subjectId)
            .OrderBy(topic => topic.NormalizedName)
            .Select(topic => new TopicResponse(
                topic.Id,
                topic.SubjectId,
                topic.Name,
                context.LiveNoticeSubjectMappings.Any(mapping => mapping.TopicId == topic.Id))
            {
                Aliases = context.TopicAliases
                    .Where(alias => alias.TopicId == topic.Id)
                    .OrderBy(alias => alias.NormalizedName)
                    .Select(alias => alias.Name)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var ids = topics.Where(topic => !topic.InUse).Select(topic => topic.Id).ToList();
        if (ids.Count == 0)
        {
            return topics;
        }

        var used = new HashSet<Guid>();
        foreach (var check in usageChecks)
        {
            used.UnionWith(await check.InUseAsync(ids, cancellationToken));
        }

        return used.Count == 0
            ? topics
            : [.. topics.Select(topic => used.Contains(topic.Id) ? topic with { InUse = true } : topic)];
    }
}
