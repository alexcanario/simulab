namespace Simulab.Catalog.Contracts;

/// <summary>What the add and the edit dialog of a subject send (F-79).</summary>
/// <param name="Name">The subject's name, 2 to 150 characters.</param>
/// <param name="AreaId">The area it belongs to, optional.</param>
/// <param name="Aliases">
/// The other names the subject goes by (F-77, BR4): the whole set, which replaces the current one. Null leaves
/// the set as it is and an empty list clears it; each name is 2 to 150 characters.
/// </param>
public sealed record SaveSubjectRequest(string? Name, Guid? AreaId = null, IReadOnlyList<string>? Aliases = null)
{
    public bool Equals(SaveSubjectRequest? other) =>
        other is not null
        && Name == other.Name
        && AreaId == other.AreaId
        && AliasLists.Equal(Aliases, other.Aliases);

    public override int GetHashCode() => HashCode.Combine(Name, AreaId, AliasLists.Hash(Aliases));
}
