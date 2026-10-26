using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Roles;

/// <summary>
/// Attaches a user to a tenant or detaches them (F-150, UC2, BR5). Any account that is not deleted can have
/// one; the change and its audit entry are written in one transaction (BR6). Roles, status and the security
/// stamp are left alone, so no session ends: the new tenant reaches the user with their next token (BR7).
/// </summary>
public sealed class SetUserTenantHandler(IRoleAdministrationStore store, IRoleAdministrationQueries queries)
{
    public async Task<Result<UserSummaryResponse>> HandleAsync(Guid userId, SetUserTenantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var saved = await store.RunExclusiveAsync(async () =>
        {
            var user = await store.FindUserAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<bool>(new Error(IdentityErrorCodes.UserNotFound, ErrorKind.NotFound));
            }

            if (request.TenantId == Guid.Empty)
            {
                return Result.Failure<bool>(new Error(IdentityErrorCodes.UserTenantInvalid, ErrorKind.Validation));
            }

            var before = user.TenantId;
            if (!user.AssignTenant(request.TenantId))
            {
                return Result.Success(true);
            }

            // BR6: written by the same save as the change, so it rolls back with it.
            store.AddRoleChange(RoleChange.UserTenantChanged(userId, before, user.TenantId));
            await store.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }, cancellationToken);

        if (saved.IsFailure)
        {
            return Result.Failure<UserSummaryResponse>(saved.Error!);
        }

        var summary = await queries.FindUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"The tenant of user {userId} was saved but the user cannot be read back.");
        return Result.Success(summary);
    }
}
