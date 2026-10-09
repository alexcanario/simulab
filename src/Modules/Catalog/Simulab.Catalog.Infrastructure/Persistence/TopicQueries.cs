using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// A subject's topics (F-79, UC5): all in one call, sorted over the normalized name so "Árvore" sorts with
/// the A's and not after Z (the F-33 lesson).
/// </summary>
public sealed class TopicQueries(CatalogModuleDbContext context) : ITopicQueries
{
    public async Task<IReadOnlyList<TopicResponse>> ListAsync(Guid subjectId, CancellationToken cancellationToken) =>
        await context.Topics
            .AsNoTracking()
            .Where(topic => topic.SubjectId == subjectId)
            .OrderBy(topic => topic.NormalizedName)
            .Select(topic => new TopicResponse(
                topic.Id,
                topic.SubjectId,
                topic.Name,
                context.LiveNoticeSubjectMappings.Any(mapping => mapping.TopicId == topic.Id)))
            .ToListAsync(cancellationToken);
}
