using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Roles;

/// <summary>
/// Deletes a custom role that nobody holds (F-9, UC4, BR5). The delete is soft: the row stays, hidden, and
/// its name stays taken (BR3). A role without holders cannot change who manages roles, so BR8b holds.
/// </summary>
public sealed class DeleteRoleHandler(IRoleAdministrationStore store)
{
    public Task<Result<bool>> HandleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        store.RunExclusiveAsync(async () =>
        {
            var role = await store.FindRoleAsync(roleId, cancellationToken);
            if (role is null)
            {
                return Failure(IdentityErrorCodes.RoleNotFound, ErrorKind.NotFound);
            }

            if (role.IsSystem)
            {
                return Failure(IdentityErrorCodes.RoleSystemRoleProtected, ErrorKind.BusinessRule);
            }

            if (await store.CountHoldersAsync(roleId, cancellationToken) > 0)
            {
                return Failure(IdentityErrorCodes.RoleInUse, ErrorKind.BusinessRule);
            }

            store.DeleteRole(role);
            store.AddRoleChange(RoleChange.RoleDeleted(role.Id, role.Name!));
            await store.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }, cancellationToken);

    private static Result<bool> Failure(string code, ErrorKind kind) => Result.Failure<bool>(new Error(code, kind));
}
