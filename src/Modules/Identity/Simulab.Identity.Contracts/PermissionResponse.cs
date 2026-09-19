namespace Simulab.Identity.Contracts;

/// <summary>
/// One entry of the permission catalog (F-9). Only the name travels: the Web shows its translated name and
/// description from resources keyed by it, grouped by <see cref="IdentityPermissions.GroupOf"/> (BR12).
/// </summary>
public sealed record PermissionResponse(string Name);
