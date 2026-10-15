namespace Simulab.Catalog.Contracts;

/// <summary>A subject as the list and the subject page show it (F-79).</summary>
/// <param name="Id">The subject.</param>
/// <param name="Name">The name as typed.</param>
/// <param name="AreaId">The area it belongs to, null when it has none.</param>
/// <param name="AreaCode">The area's stable code, null when it has none.</param>
/// <param name="TopicCount">How many topics it has, deleted ones excluded; the screen blocks the delete on it.</param>
/// <param name="InUse">True when a live notice subject maps it as a whole subject (F-75, BR10); the screen blocks the delete on it.</param>
public sealed record SubjectResponse(Guid Id, string Name, Guid? AreaId, string? AreaCode, int TopicCount, bool InUse = false)
{
    /// <summary>
    /// The other names the subject goes by (F-77, BR13), ordered by their normalized form. Empty, never null:
    /// an init property and not a positional parameter, which could only default to null.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    public bool Equals(SubjectResponse? other) =>
        other is not null
        && Id == other.Id
        && Name == other.Name
        && AreaId == other.AreaId
        && AreaCode == other.AreaCode
        && TopicCount == other.TopicCount
        && InUse == other.InUse
        && AliasLists.Equal(Aliases, other.Aliases);

    public override int GetHashCode() =>
        HashCode.Combine(Id, Name, AreaId, AreaCode, TopicCount, InUse, AliasLists.Hash(Aliases));
}
