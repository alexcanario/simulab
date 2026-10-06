using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Topics;

/// <summary>The read side of a subject's topics (F-79, UC5).</summary>
public interface ITopicQueries
{
    /// <summary>The subject's topics by normalized name, all in one call (BR11, BR12).</summary>
    Task<IReadOnlyList<TopicResponse>> ListAsync(Guid subjectId, CancellationToken cancellationToken);
}
