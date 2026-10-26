using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;

namespace Simulab.ExamEngine.Infrastructure;

/// <summary>Reads the permission to see any attempt from Identity's query, never from the token (F-166, BR7).</summary>
public sealed class AttemptAccess(IPermissionQueryService permissions) : IAttemptAccess
{
    public async Task<bool> CanReadAnyAsync(Guid userId, CancellationToken cancellationToken) =>
        (await permissions.GetEffectivePermissionsAsync(userId, cancellationToken)).Contains(ExamEnginePermissions.Read);
}
