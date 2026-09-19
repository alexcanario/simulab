using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Roles;

/// <summary>
/// Replaces the whole set of roles a user holds (F-9, UC6, BR6). Any account that is not deleted can hold
/// roles, pending ones included (BR7). The change is refused when it would leave nobody able to manage
/// roles (BR8b).
/// </summary>
public sealed class SetUserRolesHandler(IRoleAdministrationStore store, IRoleAdministrationQueries queries)
{
    public async Task<Result<UserSummaryResponse>> HandleAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var roleIds = new HashSet<Guid>(request.RoleIds ?? []);
        var saved = await store.RunExclusiveAsync(async () =>
        {
            if (!await store.UserExistsAsync(userId, cancellationToken))
            {
                return Failure(IdentityErrorCodes.UserNotFound, ErrorKind.NotFound);
            }

            if ((await store.UnknownRolesAsync(roleIds, cancellationToken)).Count > 0)
            {
                return Failure(IdentityErrorCodes.RoleAssignmentRoleUnknown, ErrorKind.Validation);
            }

            await store.ReplaceUserRolesAsync(userId, roleIds, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);

            // BR8b: judged after the change, inside the same transaction, which rolls back on failure.
            return await store.CountActiveManagersAsync(cancellationToken) == 0
                ? Failure(IdentityErrorCodes.RoleAssignmentLastManager, ErrorKind.BusinessRule)
                : Result.Success(true);
        }, cancellationToken);

        if (saved.IsFailure)
        {
            return Result.Failure<UserSummaryResponse>(saved.Error!);
        }

        var user = await queries.FindUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"The roles of user {userId} were saved but the user cannot be read back.");
        return Result.Success(user);
    }

    private static Result<bool> Failure(string code, ErrorKind kind) => Result.Failure<bool>(new Error(code, kind));
}
