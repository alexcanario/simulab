using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-90 AC9, BR9: the catalog adds what every registered <see cref="ITopicUsageCheck"/> counts for a topic, so a
/// second module that references topics adds its own answer and the catalog does not change.
/// </summary>
public sealed class TopicUsageCountTests
{
    private static readonly Guid TopicId = Guid.Parse("0198f0a3-0000-7000-8000-0000000000c1");

    [Fact]
    public async Task CountAsync_TwoChecks_AddsBothCounts()
    {
        ITopicUsageCheck[] checks = [new FixedCount(3), new FixedCount(2)];

        var count = await TopicUsageChecks.CountAsync(checks, TopicId, CancellationToken.None);

        count.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_NoCheck_IsZero()
    {
        var count = await TopicUsageChecks.CountAsync([], TopicId, CancellationToken.None);

        count.Should().Be(0);
    }

    /// <summary>A module that counts the same number of rows for every topic.</summary>
    private sealed class FixedCount(int count) : ITopicUsageCheck
    {
        public Task<IReadOnlySet<Guid>> InUseAsync(IReadOnlyCollection<Guid> topicIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(count > 0 ? topicIds.ToHashSet() : new HashSet<Guid>());

        public Task<int> CountAsync(Guid topicId, CancellationToken cancellationToken) => Task.FromResult(count);
    }
}
