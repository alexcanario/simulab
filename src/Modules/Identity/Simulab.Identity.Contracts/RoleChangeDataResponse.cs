namespace Simulab.Identity.Contracts;

/// <summary>
/// One role change whose target is the user (F-16 BR4). The actor is only <see cref="ByYou"/>: another
/// person's id or email is their data, not this user's.
/// </summary>
public sealed record RoleChangeDataResponse(
    DateTimeOffset At,
    string Action,
    string? RoleName,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    bool ByYou);
