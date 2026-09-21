namespace Simulab.Identity.Contracts;

/// <summary>
/// A permission or role added or removed (F-14, BR2). <paramref name="Key"/> is the permission name or the role
/// id; <paramref name="Name"/> the permission name or the role's name at that moment; <paramref name="IsSystem"/>
/// is true for a seed role, whose name the screen translates.
/// </summary>
public sealed record RoleChangeItemResponse(string Key, string Name, bool IsSystem);
