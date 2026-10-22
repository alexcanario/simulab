using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Topics;

/// <summary>The read side of a subject's topics (F-79, UC5).</summary>
public interface ITopicQueries
{
    /// <summary>The subject's topics by normalized name, all in one call (BR11, BR12).</summary>
    Task<IReadOnlyList<TopicResponse>> ListAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>
    /// The live notice subjects that map the topic and the count every module gives for it (F-90, BR3, BR4, BR5, BR9),
    /// or null when it does not exist or was deleted.
    /// </summary>
    Task<WhereUsedResponse?> WhereUsedAsync(Guid id, CancellationToken cancellationToken);
}
