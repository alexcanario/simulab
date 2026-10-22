using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Topics;

/// <summary>
/// Asks every registered <see cref="ITopicUsageCheck"/> whether a topic is still used (F-95, BR13). A module that
/// adds a reference to a topic registers its own check; the catalog reads this and never references the module.
/// </summary>
public static class TopicUsageChecks
{
    /// <summary>True when any check says <paramref name="topicId"/> is in use.</summary>
    public static async Task<bool> AnyAsync(
        IEnumerable<ITopicUsageCheck> checks,
        Guid topicId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);

        foreach (var check in checks)
        {
            if ((await check.InUseAsync([topicId], cancellationToken)).Contains(topicId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The sum of what every check counts for <paramref name="topicId"/> (F-90, BR9).</summary>
    public static Task<int> CountAsync(
        IEnumerable<ITopicUsageCheck> checks,
        Guid topicId,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
