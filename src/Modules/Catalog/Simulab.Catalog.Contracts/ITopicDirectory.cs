namespace Simulab.Catalog.Contracts;

/// <summary>
/// What another module may ask about topics without reading the catalog's tables (F-95). Implemented by the
/// Catalog module over its own context; a topic that does not exist or was deleted is simply absent.
/// </summary>
public interface ITopicDirectory
{
    /// <summary>The topic with its subject, or null when it does not exist or was deleted.</summary>
    Task<TopicDirectoryEntry?> FindAsync(Guid topicId, CancellationToken cancellationToken);

    /// <summary>The live topics among <paramref name="topicIds"/>; ids that are unknown or deleted are left out.</summary>
    Task<IReadOnlyList<TopicDirectoryEntry>> FindManyAsync(
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken);
}
