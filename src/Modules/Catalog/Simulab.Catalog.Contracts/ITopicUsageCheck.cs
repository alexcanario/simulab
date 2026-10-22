namespace Simulab.Catalog.Contracts;

/// <summary>
/// A module that holds a reference to a topic says, through one of these, which of them it still uses (F-95,
/// BR13). The catalog asks every registered check before it deletes a topic and when it fills the list's
/// <c>InUse</c> flag, so a module that adds a reference adds its own check and never edits the catalog.
/// </summary>
public interface ITopicUsageCheck
{
    /// <summary>The ids among <paramref name="topicIds"/> that this module still uses with a live row.</summary>
    Task<IReadOnlySet<Guid>> InUseAsync(IReadOnlyCollection<Guid> topicIds, CancellationToken cancellationToken);

    /// <summary>
    /// How many live rows of this module point at <paramref name="topicId"/>; 0 when none (F-90, BR9). It counts the
    /// same rows <see cref="InUseAsync"/> looks at, so a topic is in use exactly when some check counts more than 0.
    /// </summary>
    Task<int> CountAsync(Guid topicId, CancellationToken cancellationToken);
}
