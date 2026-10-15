namespace Simulab.Catalog.Contracts;

/// <summary>What the add and the edit dialog of a topic send (F-79).</summary>
/// <param name="Name">The topic's name, 2 to 200 characters.</param>
/// <param name="SubjectId">
/// Where the topic moves to (BR10). A create takes its subject from the route and ignores this; an update
/// with null keeps the current subject.
/// </param>
/// <param name="Aliases">
/// The other names the topic goes by (F-77, BR4): the whole set, which replaces the current one. Null leaves
/// the set as it is and an empty list clears it; each name is 2 to 200 characters.
/// </param>
public sealed record SaveTopicRequest(string? Name, Guid? SubjectId = null, IReadOnlyList<string>? Aliases = null)
{
    public bool Equals(SaveTopicRequest? other) =>
        other is not null
        && Name == other.Name
        && SubjectId == other.SubjectId
        && AliasLists.Equal(Aliases, other.Aliases);

    public override int GetHashCode() => HashCode.Combine(Name, SubjectId, AliasLists.Hash(Aliases));
}
