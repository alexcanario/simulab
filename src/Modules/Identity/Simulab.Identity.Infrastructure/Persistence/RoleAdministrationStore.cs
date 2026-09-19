using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// The back office's writes (F-9). <see cref="RunExclusiveAsync{T}"/> takes a PostgreSQL transaction-level
/// advisory lock, so role-administration changes run one at a time: the last-manager check (BR8b) then sees
/// every other change committed before it, without the retries a serializable transaction would need.
/// </summary>
public sealed class RoleAdministrationStore(IdentityModuleDbContext context, ILookupNormalizer normalizer) : IRoleAdministrationStore
{
    /// <summary>Any constant works; it only has to be the same for every role-administration write.</summary>
    private const long LockKey = 0x5349_4D55_524F_4C45; // "SIMUROLE"

    public async Task<Result<T>> RunExclusiveAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);

        var result = await work();
        if (result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        return result;
    }

    public Task<Role?> FindRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        context.Roles.SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<bool> IsNameTakenAsync(string name, Guid? exceptRoleId, CancellationToken cancellationToken = default)
    {
        var normalized = normalizer.NormalizeName(name);
        return context.Roles
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .AnyAsync(role => role.NormalizedName == normalized && role.Id != exceptRoleId, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> UnknownPermissionsAsync(IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var known = await context.Permissions
            .Where(permission => permissions.Contains(permission.Name))
            .Select(permission => permission.Name)
            .ToListAsync(cancellationToken);
        return [.. permissions.Except(known, StringComparer.Ordinal)];
    }

    public async Task<IReadOnlyList<Guid>> UnknownRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        var known = await context.Roles
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);
        return [.. roleIds.Except(known)];
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(user => user.Id == userId, cancellationToken);

    public Task<int> CountHoldersAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        (from userRole in context.UserRoles
         join user in context.Users on userRole.UserId equals user.Id
         where userRole.RoleId == roleId
         select user.Id)
        .CountAsync(cancellationToken);

    public Task<int> CountActiveManagersAsync(CancellationToken cancellationToken = default) =>
        (from user in context.Users
         where user.Status == AccountStatus.Active
         where (from userRole in context.UserRoles
                join role in context.Roles on userRole.RoleId equals role.Id
                join rolePermission in context.RolePermissions on role.Id equals rolePermission.RoleId
                where userRole.UserId == user.Id && rolePermission.PermissionName == IdentityPermissions.RolesManage
                select userRole).Any()
         select user.Id)
        .CountAsync(cancellationToken);

    public void AddRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.NormalizedName = normalizer.NormalizeName(role.Name);
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        context.Roles.Add(role);
    }

    public void Rename(Role role, string name)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Name = name;
        role.NormalizedName = normalizer.NormalizeName(name);
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void DeleteRole(Role role) => context.Roles.Remove(role);

    public async Task ReplacePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var current = await context.RolePermissions.Where(grant => grant.RoleId == roleId).ToListAsync(cancellationToken);
        context.RolePermissions.RemoveRange(current.Where(grant => !permissions.Contains(grant.PermissionName)));

        var kept = current.Select(grant => grant.PermissionName).ToHashSet(StringComparer.Ordinal);
        context.RolePermissions.AddRange(permissions
            .Where(name => !kept.Contains(name))
            .Select(name => new RolePermission { RoleId = roleId, PermissionName = name }));
    }

    public async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var current = await context.UserRoles.Where(userRole => userRole.UserId == userId).ToListAsync(cancellationToken);
        context.UserRoles.RemoveRange(current.Where(userRole => !roleIds.Contains(userRole.RoleId)));

        var kept = current.Select(userRole => userRole.RoleId).ToHashSet();
        context.UserRoles.AddRange(roleIds
            .Where(id => !kept.Contains(id))
            .Select(id => new IdentityUserRole<Guid> { UserId = userId, RoleId = id }));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
