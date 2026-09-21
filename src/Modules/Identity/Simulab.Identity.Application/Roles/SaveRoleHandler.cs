using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Roles;

/// <summary>
/// Creates a custom role or replaces a role's name and permissions (F-9, UC2-UC3). Input checks come first
/// (BR3, BR4), then the rules that need data run inside one exclusive transaction (BR3 uniqueness, BR8).
/// </summary>
public sealed class SaveRoleHandler(IRoleAdministrationStore store, IRoleAdministrationQueries queries)
{
    public async Task<Result<RoleResponse>> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = RoleName.Normalize(request.Name);
        if (name is null)
        {
            return Failure(IdentityErrorCodes.RoleNameInvalid, ErrorKind.Validation);
        }

        var permissions = Distinct(request.Permissions);
        if ((await store.UnknownPermissionsAsync(permissions, cancellationToken)).Count > 0)
        {
            return Failure(IdentityErrorCodes.RolePermissionUnknown, ErrorKind.Validation);
        }

        var role = Role.CreateCustom(name);
        var saved = await store.RunExclusiveAsync(async () =>
        {
            if (await store.IsNameTakenAsync(name, exceptRoleId: null, cancellationToken))
            {
                return Failure<Guid>(IdentityErrorCodes.RoleNameTaken, ErrorKind.Conflict);
            }

            store.AddRole(role);
            await store.ReplacePermissionsAsync(role.Id, permissions, cancellationToken);
            store.AddRoleChange(RoleChange.RoleCreated(role.Id, name, permissions));
            await store.SaveChangesAsync(cancellationToken);

            // A new role has no holder yet, so it cannot change who manages roles (BR8b).
            return Result.Success(role.Id);
        }, cancellationToken);

        return await AnswerAsync(saved, cancellationToken);
    }

    public async Task<Result<RoleResponse>> UpdateAsync(Guid roleId, SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var permissions = Distinct(request.Permissions);
        var saved = await store.RunExclusiveAsync(async () =>
        {
            var role = await store.FindRoleAsync(roleId, cancellationToken);
            if (role is null)
            {
                return Failure<Guid>(IdentityErrorCodes.RoleNotFound, ErrorKind.NotFound);
            }

            var name = RoleName.Normalize(request.Name);
            if (name is null)
            {
                return Failure<Guid>(IdentityErrorCodes.RoleNameInvalid, ErrorKind.Validation);
            }

            // BR2: the dialog sends a system role's name back unchanged; anything else is a rename.
            if (role.IsSystem && !string.Equals(name, role.Name, StringComparison.Ordinal))
            {
                return Failure<Guid>(IdentityErrorCodes.RoleSystemRoleProtected, ErrorKind.BusinessRule);
            }

            if ((await store.UnknownPermissionsAsync(permissions, cancellationToken)).Count > 0)
            {
                return Failure<Guid>(IdentityErrorCodes.RolePermissionUnknown, ErrorKind.Validation);
            }

            // BR8a: the Admin role always keeps the permission that manages roles.
            if (role.IsSystem && role.Name == IdentityRoles.Admin && !permissions.Contains(IdentityPermissions.RolesManage))
            {
                return Failure<Guid>(IdentityErrorCodes.RoleAdminPermissionRequired, ErrorKind.BusinessRule);
            }

            var nameBefore = role.Name!;
            if (!role.IsSystem && name != role.Name)
            {
                if (await store.IsNameTakenAsync(name, role.Id, cancellationToken))
                {
                    return Failure<Guid>(IdentityErrorCodes.RoleNameTaken, ErrorKind.Conflict);
                }

                store.Rename(role, name);
            }

            var permissionsBefore = await store.PermissionsOfAsync(role.Id, cancellationToken);
            await store.ReplacePermissionsAsync(role.Id, permissions, cancellationToken);
            if (RoleChange.RoleUpdated(role.Id, nameBefore, role.Name!, permissionsBefore, permissions) is { } change)
            {
                store.AddRoleChange(change);
            }

            await store.SaveChangesAsync(cancellationToken);

            // BR8b: judged after the change, inside the same transaction, which rolls back on failure - the
            // F-14 audit entry with it (BR3).
            return await store.CountActiveManagersAsync(cancellationToken) == 0
                ? Failure<Guid>(IdentityErrorCodes.RoleAssignmentLastManager, ErrorKind.BusinessRule)
                : Result.Success(role.Id);
        }, cancellationToken);

        return await AnswerAsync(saved, cancellationToken);
    }

    private async Task<Result<RoleResponse>> AnswerAsync(Result<Guid> saved, CancellationToken cancellationToken)
    {
        if (saved.IsFailure)
        {
            return Result.Failure<RoleResponse>(saved.Error!);
        }

        var response = await queries.FindRoleAsync(saved.Value, cancellationToken)
            ?? throw new InvalidOperationException($"The role {saved.Value} was saved but cannot be read back.");
        return Result.Success(response);
    }

    private static HashSet<string> Distinct(IReadOnlyList<string>? permissions) =>
        new(permissions ?? [], StringComparer.Ordinal);

    private static Result<RoleResponse> Failure(string code, ErrorKind kind) => Result.Failure<RoleResponse>(new Error(code, kind));

    private static Result<T> Failure<T>(string code, ErrorKind kind) => Result.Failure<T>(new Error(code, kind));
}
