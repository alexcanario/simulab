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
}
