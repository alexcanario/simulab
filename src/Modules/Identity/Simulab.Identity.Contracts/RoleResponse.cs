namespace Simulab.Identity.Contracts;

/// <summary>
/// One role as the back office shows it (F-9, UC1). <paramref name="Name"/> is the stored name; the Web
/// translates the three system roles by their name (BR12).
/// </summary>
public sealed record RoleResponse(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UserCount);
