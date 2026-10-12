namespace Simulab.Catalog.Contracts;

/// <summary>
/// What another module may ask about subjects and the topics under them without reading the catalog's tables
/// (B-28). Implemented by the Catalog module over its own context; a subject or a topic that does not exist or
/// was deleted is simply absent. It answers "which subjects match this text, and what are the topics of this
/// one", the question <see cref="ITopicDirectory"/> does not.
/// </summary>
public interface ISubjectDirectory
{
    /// <summary>
    /// Live subjects whose name contains <paramref name="search"/> (case and accents ignored), by name, at most
    /// <paramref name="take"/>. An empty search lists the first ones; a subject with no topic is listed too.
    /// </summary>
    Task<IReadOnlyList<SubjectDirectoryEntry>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// The live topics of the subject by name, or null when the subject does not exist or was deleted (a live
    /// subject with no topic answers an empty list).
    /// </summary>
    Task<IReadOnlyList<TopicDirectoryEntry>?> ListTopicsAsync(Guid subjectId, CancellationToken cancellationToken);
}
