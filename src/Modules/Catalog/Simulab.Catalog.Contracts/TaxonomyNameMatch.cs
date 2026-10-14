namespace Simulab.Catalog.Contracts;

/// <summary>
/// What a name matches in the taxonomy (F-77, BR11): the live subjects and the live topics whose name or other
/// name has this normalized form. It does not say which of the two matched.
/// </summary>
/// <param name="Subjects">Zero or one subject, because a subject's names and other names are unique (BR6).</param>
/// <param name="Topics">Zero or more topics, each with its subject, by subject name then topic name (BR7).</param>
public sealed record TaxonomyNameMatch(
    IReadOnlyList<SubjectDirectoryEntry> Subjects,
    IReadOnlyList<TopicDirectoryEntry> Topics)
{
    /// <summary>Nothing matched.</summary>
    public static TaxonomyNameMatch Empty { get; } = new([], []);
}
