namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// One permission or role added or removed by a <see cref="RoleChange"/> (F-14, BR2). <see cref="Key"/> is the
/// permission name or the role id; <see cref="Name"/> is the permission name or the role's name at that moment.
/// </summary>
public sealed class RoleChangeItem
{
    public required string Key { get; init; }

    public required string Name { get; init; }
}
