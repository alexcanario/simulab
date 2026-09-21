using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// One recorded change to a role or to a user's roles (F-14): the audit trail of the back office. The author
/// and the time are the audit fields (<see cref="TenantEntity.CreatedBy"/>, <see cref="TenantEntity.CreatedAt"/>)
/// filled on save. It holds ids and role or permission names, never a user's email or name (BR5), and it is
/// never changed after it is written (BR4). The factories return null when nothing changed (BR3).
/// </summary>
public sealed class RoleChange : TenantEntity
{
    private RoleChange()
    {
    }

    public RoleChangeAction Action { get; private set; }

    /// <summary>The role the change is about; null for a change of a user's roles.</summary>
    public Guid? RoleId { get; private set; }

    /// <summary>The role's name at that moment (after a rename, the new name).</summary>
    public string? RoleName { get; private set; }

    /// <summary>The user whose roles changed; null for a change of a role.</summary>
    public Guid? TargetUserId { get; private set; }

    public string? NameBefore { get; private set; }

    public string? NameAfter { get; private set; }

    public List<RoleChangeItem> Added { get; private set; } = [];

    public List<RoleChangeItem> Removed { get; private set; } = [];

    /// <summary>Every role the entry is about (BR6): the role itself, or the roles added to and removed from the user.</summary>
    public List<Guid> RoleIds { get; private set; } = [];

    public static RoleChange RoleCreated(Guid roleId, string name, IEnumerable<string> permissions) =>
        new()
        {
            Action = RoleChangeAction.RoleCreated,
            RoleId = roleId,
            RoleName = name,
            Added = Permissions(permissions),
            RoleIds = [roleId]
        };

    public static RoleChange? RoleUpdated(
        Guid roleId,
        string nameBefore,
        string nameAfter,
        IReadOnlyCollection<string> permissionsBefore,
        IReadOnlyCollection<string> permissionsAfter)
    {
        ArgumentNullException.ThrowIfNull(permissionsBefore);
        ArgumentNullException.ThrowIfNull(permissionsAfter);

        var renamed = !string.Equals(nameBefore, nameAfter, StringComparison.Ordinal);
        var added = Permissions(permissionsAfter.Except(permissionsBefore, StringComparer.Ordinal));
        var removed = Permissions(permissionsBefore.Except(permissionsAfter, StringComparer.Ordinal));
        if (!renamed && added.Count == 0 && removed.Count == 0)
        {
            return null;
        }

        return new RoleChange
        {
            Action = RoleChangeAction.RoleUpdated,
            RoleId = roleId,
            RoleName = nameAfter,
            NameBefore = renamed ? nameBefore : null,
            NameAfter = renamed ? nameAfter : null,
            Added = added,
            Removed = removed,
            RoleIds = [roleId]
        };
    }

    public static RoleChange RoleDeleted(Guid roleId, string name) =>
        new()
        {
            Action = RoleChangeAction.RoleDeleted,
            RoleId = roleId,
            RoleName = name,
            RoleIds = [roleId]
        };

    /// <param name="userId">The user whose roles changed.</param>
    /// <param name="rolesBefore">The roles the user held, by id, with their names.</param>
    /// <param name="rolesAfter">The roles the user holds now, by id, with their names.</param>
    public static RoleChange? UserRolesChanged(
        Guid userId,
        IReadOnlyDictionary<Guid, string> rolesBefore,
        IReadOnlyDictionary<Guid, string> rolesAfter)
    {
        ArgumentNullException.ThrowIfNull(rolesBefore);
        ArgumentNullException.ThrowIfNull(rolesAfter);

        var added = rolesAfter.Where(role => !rolesBefore.ContainsKey(role.Key)).ToList();
        var removed = rolesBefore.Where(role => !rolesAfter.ContainsKey(role.Key)).ToList();
        if (added.Count == 0 && removed.Count == 0)
        {
            return null;
        }

        return new RoleChange
        {
            Action = RoleChangeAction.UserRolesChanged,
            TargetUserId = userId,
            Added = Roles(added),
            Removed = Roles(removed),
            RoleIds = [.. added.Concat(removed).Select(role => role.Key)]
        };
    }

    private static List<RoleChangeItem> Permissions(IEnumerable<string> names) =>
        [.. names.Order(StringComparer.Ordinal).Select(name => new RoleChangeItem { Key = name, Name = name })];

    private static List<RoleChangeItem> Roles(IEnumerable<KeyValuePair<Guid, string>> roles) =>
        [.. roles
            .OrderBy(role => role.Value, StringComparer.OrdinalIgnoreCase)
            .Select(role => new RoleChangeItem { Key = role.Key.ToString(), Name = role.Value })];
}
