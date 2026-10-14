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

    /// <summary>
    /// The live taxonomy (F-100): every live subject by name with its live topics by name, or only the live
    /// subjects among <paramref name="subjectIds"/> when it is given. A subject that is unknown or deleted is
    /// absent from the answer; a live subject with no topic is present with an empty list, which is how a
    /// caller tells "unknown" from "known and empty".
    /// </summary>
    Task<IReadOnlyList<TaxonomySubjectResponse>> ListTaxonomyAsync(
        IReadOnlyCollection<Guid>? subjectIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// What a name stands for in the taxonomy (F-77, BR11): every live subject whose name or other name has the
    /// same normalized form as <paramref name="name"/> (case and accents ignored), and every live topic whose
    /// name or other name does, each with its subject. An empty name matches nothing. The answer does not say
    /// whether the name or an other name matched; a deleted subject, topic or other name is never returned.
    /// </summary>
    Task<TaxonomyNameMatch> FindByNameAsync(string? name, CancellationToken cancellationToken);
}
