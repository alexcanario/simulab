using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// The back office's lists (F-9, UC1, UC5; F-14, UC2), read with the module's filters on: deleted roles and deleted
/// accounts never show, and a deleted account does not count as a role's holder.
/// </summary>
public sealed class RoleAdministrationQueries(IdentityModuleDbContext context, TimeProvider timeProvider) : IRoleAdministrationQueries
{
    public async Task<RoleChangePageResponse> ListRoleChangesAsync(RoleChangeListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var changes = context.RoleChanges.AsNoTracking();

        if (query.RoleId is { } roleId)
        {
            changes = changes.Where(change => change.RoleIds.Contains(roleId));
        }

        if (query.UserId is { } userId)
        {
            changes = changes.Where(change => change.TargetUserId == userId);
        }

        if (query.AuthorId is { } authorId)
        {
            changes = changes.Where(change => change.CreatedBy == authorId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{Escape(query.Search.Trim())}%";
            var matching = context.Users
                .Where(user => EF.Functions.ILike(user.Email!, pattern, "\\")
                    || (user.FullName != null && EF.Functions.ILike(user.FullName, pattern, "\\")))
                .Select(user => (Guid?)user.Id);
            changes = changes.Where(change => matching.Contains(change.TargetUserId));
        }

        if (query.Days is { } days)
        {
            var since = timeProvider.GetUtcNow().AddDays(-days);
            changes = changes.Where(change => change.CreatedAt >= since);
        }

        var total = await changes.CountAsync(cancellationToken);

        changes = query.Ascending
            ? changes.OrderBy(change => change.CreatedAt).ThenBy(change => change.Id)
            : changes.OrderByDescending(change => change.CreatedAt).ThenByDescending(change => change.Id);

        var pageSize = Math.Clamp(query.PageSize, 1, RoleChangeListQuery.MaxPageSize);
        var page = Math.Max(query.Page, 0);
        var rows = await changes.Skip(page * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var emails = await EmailsAsync(
            [.. rows.SelectMany(row => new[] { row.CreatedBy, row.TargetUserId }).OfType<Guid>().Distinct()],
            cancellationToken);
        var systemRoles = (await context.Roles.AsNoTracking()
                .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
                .Where(role => role.IsSystem)
                .Select(role => role.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        RoleChangeUserResponse? Person(Guid? id) => id is { } value ? new RoleChangeUserResponse(value, emails.GetValueOrDefault(value)) : null;

        IReadOnlyList<RoleChangeItemResponse> Items(IEnumerable<RoleChangeItem> items) =>
            [.. items.Select(item => new RoleChangeItemResponse(
                item.Key,
                item.Name,
                Guid.TryParse(item.Key, out var id) && systemRoles.Contains(id)))];

        var items = rows
            .Select(row => new RoleChangeResponse(
                row.Id,
                row.CreatedAt,
                Person(row.CreatedBy),
                row.Action.ToString(),
                row.RoleId is { } id ? new UserRoleResponse(id, row.RoleName ?? string.Empty, systemRoles.Contains(id)) : null,
                Person(row.TargetUserId),
                row.NameBefore,
                row.NameAfter,
                Items(row.Added),
                Items(row.Removed)))
            .ToList();

        return new RoleChangePageResponse(items, total);
    }

    public async Task<RoleChangeFiltersResponse> RoleChangeFiltersAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.RoleChanges.SelectMany(change => change.RoleIds).Distinct().ToListAsync(cancellationToken);
        var roles = await context.Roles.AsNoTracking()
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .Where(role => !role.IsDeleted || roleIds.Contains(role.Id))
            .Select(role => new RoleChangeFilterRoleResponse(role.Id, role.Name!, role.IsSystem, role.IsDeleted))
            .ToListAsync(cancellationToken);

        var authorIds = await context.RoleChanges
            .Where(change => change.CreatedBy != null)
            .Select(change => change.CreatedBy!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var emails = await EmailsAsync([.. (userId is { } filtered ? authorIds.Append(filtered) : authorIds).Distinct()], cancellationToken);
        var authors = authorIds
            .Select(id => new RoleChangeUserResponse(id, emails.GetValueOrDefault(id)))
            .OrderBy(author => author.Email is null)
            .ThenBy(author => author.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RoleChangeFiltersResponse(
            [.. roles.OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)],
            authors,
            userId is { } id ? new RoleChangeUserResponse(id, emails.GetValueOrDefault(id)) : null);
    }

    /// <summary>F-14, BR5: emails read at display time; an erased (soft-deleted) or unknown account has none.</summary>
    private async Task<Dictionary<Guid, string?>> EmailsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await context.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.Email })
            .ToDictionaryAsync(user => user.Id, user => user.Email, cancellationToken);

    public async Task<IReadOnlyList<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await RolesAsync(roleId: null, cancellationToken);

    public async Task<RoleResponse?> FindRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        (await RolesAsync(roleId, cancellationToken)).SingleOrDefault();

    public async Task<IReadOnlyList<string>> ListPermissionsAsync(CancellationToken cancellationToken = default) =>
        await context.Permissions.OrderBy(permission => permission.Name).Select(permission => permission.Name).ToListAsync(cancellationToken);

    public async Task<UserPageResponse> ListUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{Escape(query.Search.Trim())}%";
            users = users.Where(user =>
                EF.Functions.ILike(user.Email!, pattern, "\\")
                || (user.FullName != null && EF.Functions.ILike(user.FullName, pattern, "\\")));
        }

        if (query.RoleId is { } roleId)
        {
            users = users.Where(user => context.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleId));
        }

        var total = await users.CountAsync(cancellationToken);

        var byName = string.Equals(query.SortBy, UserListQuery.SortByName, StringComparison.OrdinalIgnoreCase);
        users = (byName, query.Descending) switch
        {
            (true, false) => users.OrderBy(user => user.FullName).ThenBy(user => user.Email),
            (true, true) => users.OrderByDescending(user => user.FullName).ThenBy(user => user.Email),
            (false, true) => users.OrderByDescending(user => user.Email),
            _ => users.OrderBy(user => user.Email)
        };

        var pageSize = Math.Clamp(query.PageSize, 1, UserListQuery.MaxPageSize);
        var page = Math.Max(query.Page, 0);
        var rows = await users
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(user => new { user.Id, Email = user.Email!, user.FullName, user.Status })
            .ToListAsync(cancellationToken);

        var roles = await RolesOfAsync([.. rows.Select(row => row.Id)], cancellationToken);
        var items = rows
            .Select(row => new UserSummaryResponse(row.Id, row.Email, row.FullName, row.Status.ToString(), roles.GetValueOrDefault(row.Id, [])))
            .ToList();

        return new UserPageResponse(items, total);
    }

    public async Task<UserSummaryResponse?> FindUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Id, Email = user.Email!, user.FullName, user.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var roles = await RolesOfAsync([userId], cancellationToken);
        return new UserSummaryResponse(row.Id, row.Email, row.FullName, row.Status.ToString(), roles.GetValueOrDefault(userId, []));
    }

    private async Task<List<RoleResponse>> RolesAsync(Guid? roleId, CancellationToken cancellationToken)
    {
        var roles = await context.Roles.AsNoTracking()
            .Where(role => roleId == null || role.Id == roleId)
            .Select(role => new
            {
                role.Id,
                Name = role.Name!,
                role.IsSystem,
                Permissions = context.RolePermissions
                    .Where(grant => grant.RoleId == role.Id)
                    .OrderBy(grant => grant.PermissionName)
                    .Select(grant => grant.PermissionName)
                    .ToList(),
                UserCount = (from userRole in context.UserRoles
                             join user in context.Users on userRole.UserId equals user.Id
                             where userRole.RoleId == role.Id
                             select user.Id).Count()
            })
            .ToListAsync(cancellationToken);

        return [.. roles
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .Select(role => new RoleResponse(role.Id, role.Name, role.IsSystem, role.Permissions, role.UserCount))];
    }

    private async Task<Dictionary<Guid, IReadOnlyList<UserRoleResponse>>> RolesOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Id, Name = role.Name!, role.IsSystem })
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(pair => pair.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<UserRoleResponse>)[.. group
                    .OrderBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => new UserRoleResponse(pair.Id, pair.Name, pair.IsSystem))]);
    }

    /// <summary>The search term is text, not a pattern: <c>%</c>, <c>_</c> and the escape itself match literally.</summary>
    private static string Escape(string term) =>
        term.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
