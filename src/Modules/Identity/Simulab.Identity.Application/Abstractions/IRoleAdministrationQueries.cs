using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>The back office's reads (F-9, UC1, UC5): what the two screens list. Deleted roles and accounts are never listed.</summary>
public interface IRoleAdministrationQueries
{
    Task<IReadOnlyList<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse?> FindRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>The permission catalog, by name.</summary>
    Task<IReadOnlyList<string>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<UserPageResponse> ListUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<UserSummaryResponse?> FindUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>F-14, UC2: one page of the role history. <see cref="RoleChangeListQuery.Days"/> is already checked.</summary>
    Task<RoleChangePageResponse> ListRoleChangesAsync(RoleChangeListQuery query, CancellationToken cancellationToken = default);

    /// <summary>F-14, BR8: every current role and the deleted ones found in the history, the authors found in it, and the user <paramref name="userId"/> when given.</summary>
    Task<RoleChangeFiltersResponse> RoleChangeFiltersAsync(Guid? userId, CancellationToken cancellationToken = default);
}
