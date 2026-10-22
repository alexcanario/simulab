using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question bank's answer to the catalog's delete guard (F-95, BR13): a topic is in use while at least
/// one live question of any status points at it. Deleted questions are left out by the soft-delete filter.
/// </summary>
public sealed class QuestionTopicUsageCheck(QuestionBankModuleDbContext context) : ITopicUsageCheck
{
    public async Task<IReadOnlySet<Guid>> InUseAsync(
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(topicIds);

        if (topicIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = topicIds.ToList();
        var used = await context.Questions
            .AsNoTracking()
            .Where(question => ids.Contains(question.TopicId))
            .Select(question => question.TopicId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return used.ToHashSet();
    }

    // F-90, BR3: the same rows InUseAsync looks at - live questions of any status - so the count is above 0
    // exactly when the topic is in use for this module.
    public Task<int> CountAsync(Guid topicId, CancellationToken cancellationToken) =>
        context.Questions
            .AsNoTracking()
            .CountAsync(question => question.TopicId == topicId, cancellationToken);
}
