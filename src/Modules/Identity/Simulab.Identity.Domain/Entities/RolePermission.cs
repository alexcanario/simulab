namespace Simulab.Identity.Domain.Entities;

/// <summary>Grants one <see cref="Permission"/> to one <see cref="Role"/> (F-6, BR1). Composite key: (RoleId, PermissionName).</summary>
#pragma warning disable CA1711 // "RolePermission" is the glossary term (docs/glossary.md); not a collection/flags-style suffix here.
public sealed class RolePermission
#pragma warning restore CA1711
{
    public required Guid RoleId { get; init; }

    public required string PermissionName { get; init; }

    public Role? Role { get; init; }

    public Permission? Permission { get; init; }
}
