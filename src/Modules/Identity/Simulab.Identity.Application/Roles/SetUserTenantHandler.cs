using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Roles;

/// <summary>
/// Attaches a user to a tenant or detaches them (F-150, UC2, BR5). Any account that is not deleted can have
/// one; the change and its audit entry are written in one transaction (BR6).
/// </summary>
public sealed class SetUserTenantHandler(IRoleAdministrationStore store, IRoleAdministrationQueries queries)
{
    public Task<Result<UserSummaryResponse>> HandleAsync(Guid userId, SetUserTenantRequest request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
