namespace Simulab.Catalog.Contracts;

/// <summary>A topic as the subject page shows it (F-79).</summary>
/// <param name="Id">The topic.</param>
/// <param name="SubjectId">The subject it sits under.</param>
/// <param name="Name">The name as typed.</param>
/// <param name="InUse">True when a live notice subject maps this topic (F-75, BR10); the screen blocks the delete on it.</param>
public sealed record TopicResponse(Guid Id, Guid SubjectId, string Name, bool InUse = false)
{
    /// <summary>
    /// The other names the topic goes by (F-77, BR13), ordered by their normalized form. Empty, never null.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    public bool Equals(TopicResponse? other) =>
        other is not null
        && Id == other.Id
        && SubjectId == other.SubjectId
        && Name == other.Name
        && InUse == other.InUse
        && AliasLists.Equal(Aliases, other.Aliases);

    public override int GetHashCode() => HashCode.Combine(Id, SubjectId, Name, InUse, AliasLists.Hash(Aliases));
}
