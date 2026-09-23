using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Roles;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Api;

/// <summary>
/// The role management back office (F-9): roles, the permission catalog, users and their roles. Every route
/// requires <c>identity.roles.manage</c> (BR10); a caller without it gets 403 <c>identity.forbidden</c> (F-6).
/// </summary>
public static class RoleAdministrationEndpoints
{
    public static RouteGroupBuilder MapRoleAdministrationEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var policy = PermissionPolicy.NameFor(IdentityPermissions.RolesManage);

        var permissions = group.MapGroup("/permissions").RequireAuthorization(policy);
        permissions.MapGet(string.Empty, ListPermissionsAsync).WithName("ListPermissions");

        var roles = group.MapGroup("/roles").RequireAuthorization(policy);
        roles.MapGet(string.Empty, ListRolesAsync).WithName("ListRoles");
        roles.MapPost(string.Empty, CreateRoleAsync).WithName("CreateRole");
        roles.MapPut("/{id:guid}", UpdateRoleAsync).WithName("UpdateRole");
        roles.MapDelete("/{id:guid}", DeleteRoleAsync).WithName("DeleteRole");

        var users = group.MapGroup("/users").RequireAuthorization(policy);
        users.MapGet(string.Empty, ListUsersAsync).WithName("ListUsers");
        users.MapPut("/{id:guid}/roles", SetUserRolesAsync).WithName("SetUserRoles");

        // F-14, BR7: the role history is read with the same permission.
        var roleChanges = group.MapGroup("/role-changes").RequireAuthorization(policy);
        roleChanges.MapGet(string.Empty, ListRoleChangesAsync).WithName("ListRoleChanges");
        roleChanges.MapGet("/filters", RoleChangeFiltersAsync).WithName("RoleChangeFilters");

        // F-21, BR11: the account event trail is read with the same permission.
        var accountEvents = group.MapGroup("/account-events").RequireAuthorization(policy);
        accountEvents.MapGet(string.Empty, ListAccountEventsAsync).WithName("ListAccountEvents");

        return group;
    }

    /// <summary>F-21, UC2: one page of the account event trail, newest first.</summary>
    private static async Task<IResult> ListAccountEventsAsync(
        IAccountEventQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = 25,
        Guid? user = null,
        string? @event = null,
        string? ip = null,
        string? search = null,
        int? days = null,
        bool ascending = false)
    {
        if (days is { } value && !AccountEventListQuery.Periods.Contains(value))
        {
            return IdentityEndpoints.Problem(new Error(IdentityErrorCodes.AccountEventPeriodInvalid, ErrorKind.Validation));
        }

        if (!string.IsNullOrWhiteSpace(@event) && !AccountEventTypes.All.Contains(@event))
        {
            return IdentityEndpoints.Problem(new Error(IdentityErrorCodes.AccountEventTypeInvalid, ErrorKind.Validation));
        }

        var query = new AccountEventListQuery(page, pageSize, user, @event, ip, search, days, ascending);
        return Results.Ok(await queries.ListAsync(query, cancellationToken));
    }

    private static async Task<IResult> ListRoleChangesAsync(
        IRoleAdministrationQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = 25,
        Guid? roleId = null,
        Guid? userId = null,
        Guid? authorId = null,
        string? search = null,
        int? days = null,
        bool ascending = false)
    {
        if (days is { } value && !RoleChangeListQuery.Periods.Contains(value))
        {
            return IdentityEndpoints.Problem(new Error(IdentityErrorCodes.RoleChangePeriodInvalid, ErrorKind.Validation));
        }

        var query = new RoleChangeListQuery(page, pageSize, roleId, userId, authorId, search, days, ascending);
        return Results.Ok(await queries.ListRoleChangesAsync(query, cancellationToken));
    }

    private static async Task<IResult> RoleChangeFiltersAsync(IRoleAdministrationQueries queries, CancellationToken cancellationToken, Guid? userId = null) =>
        Results.Ok(await queries.RoleChangeFiltersAsync(userId, cancellationToken));

    private static async Task<IResult> ListPermissionsAsync(IRoleAdministrationQueries queries, CancellationToken cancellationToken)
    {
        var names = await queries.ListPermissionsAsync(cancellationToken);
        return Results.Ok(names.Select(name => new PermissionResponse(name)).ToList());
    }

    private static async Task<IResult> ListRolesAsync(IRoleAdministrationQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.ListRolesAsync(cancellationToken));

    private static async Task<IResult> CreateRoleAsync(SaveRoleRequest request, SaveRoleHandler handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await handler.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/identity/roles/{result.Value.Id}", result.Value)
            : IdentityEndpoints.Problem(result.Error!);
    }

    private static async Task<IResult> UpdateRoleAsync(Guid id, SaveRoleRequest request, SaveRoleHandler handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Answer(await handler.UpdateAsync(id, request, cancellationToken));
    }

    private static async Task<IResult> DeleteRoleAsync(Guid id, DeleteRoleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : IdentityEndpoints.Problem(result.Error!);
    }

    private static async Task<IResult> ListUsersAsync(
        IRoleAdministrationQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = 25,
        string? search = null,
        Guid? roleId = null,
        string? sortBy = null,
        bool descending = false) =>
        Results.Ok(await queries.ListUsersAsync(new UserListQuery(page, pageSize, search, roleId, sortBy, descending), cancellationToken));

    private static async Task<IResult> SetUserRolesAsync(Guid id, SetUserRolesRequest request, SetUserRolesHandler handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Answer(await handler.HandleAsync(id, request, cancellationToken));
    }

    private static IResult Answer<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : IdentityEndpoints.Problem(result.Error!);
}
