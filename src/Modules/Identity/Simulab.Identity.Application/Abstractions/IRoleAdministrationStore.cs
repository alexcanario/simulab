using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// The back office's writes and the checks they need (F-9). Every write runs inside
/// <see cref="RunExclusiveAsync{T}"/>: one role-administration change at a time, in one transaction that is
/// rolled back when the work returns a failure - so the "never zero managers" check (BR8b) sees the change
/// it judges and two Admins acting at once cannot both remove the last manager.
/// </summary>
public interface IRoleAdministrationStore
{
    Task<Result<T>> RunExclusiveAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken = default);

    /// <summary>A role that is not deleted, tracked for changes.</summary>
    Task<Role?> FindRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>BR3: compares case-insensitively with every role, deleted ones included.</summary>
    Task<bool> IsNameTakenAsync(string name, Guid? exceptRoleId, CancellationToken cancellationToken = default);

    /// <summary>The names in <paramref name="permissions"/> that are not in the catalog.</summary>
    Task<IReadOnlyList<string>> UnknownPermissionsAsync(IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>The ids in <paramref name="roleIds"/> that are not an existing, non-deleted role.</summary>
    Task<IReadOnlyList<Guid>> UnknownRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>A non-deleted account of any status (BR7).</summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountHoldersAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>BR8b: Active, non-deleted accounts that hold <c>identity.roles.manage</c> through a non-deleted role.</summary>
    Task<int> CountActiveManagersAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new role with its name normalized the way ASP.NET Identity does.</summary>
    void AddRole(Role role);

    void Rename(Role role, string name);

    void DeleteRole(Role role);

    /// <summary>F-14: the permission names a role grants now, before a change replaces them.</summary>
    Task<IReadOnlyList<string>> PermissionsOfAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>F-14: the non-deleted roles a user holds now, by id, with their names.</summary>
    Task<IReadOnlyDictionary<Guid, string>> RolesOfUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>F-14: the names of the given non-deleted roles, by id.</summary>
    Task<IReadOnlyDictionary<Guid, string>> RoleNamesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>F-14: stages an audit trail entry, written by the same save as the change it records (BR3).</summary>
    void AddRoleChange(RoleChange change);

    Task ReplacePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default);

    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
