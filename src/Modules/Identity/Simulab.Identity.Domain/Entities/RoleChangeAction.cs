namespace Simulab.Identity.Domain.Entities;

/// <summary>What a <see cref="RoleChange"/> records (F-14, BR1). Stored as text.</summary>
public enum RoleChangeAction
{
    RoleCreated,
    RoleUpdated,
    RoleDeleted,
    UserRolesChanged
}
