using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Persistence;

namespace Simulab.Identity.Infrastructure.Authorization;

/// <summary>
/// Reads the caller's effective permissions from its roles (F-6, BR3), cached per user for 10 seconds -
/// short enough that a revoked permission takes effect almost immediately, long enough that a page with
/// several permission checks does not query the database once per check.
/// </summary>
/// <remarks>
/// Reads <paramref name="timeProvider"/> instead of <c>IMemoryCache</c> (whose expiration clock is not a
/// <see cref="TimeProvider"/> and cannot be swapped for a test's fake one), so a test proves the 10
/// second expiry by moving the clock instead of sleeping.
/// </remarks>
public sealed class PermissionQueryService(IdentityModuleDbContext context, PermissionCache cache, TimeProvider timeProvider) : IPermissionQueryService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (cache.TryGet(userId, now, out var cached))
        {
            return cached;
        }

        var permissions = await (
            from userRole in context.UserRoles
            where userRole.UserId == userId
            join rolePermission in context.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            select rolePermission.PermissionName)
            .Distinct()
            .ToListAsync(cancellationToken);

        cache.Set(userId, permissions, now + CacheDuration);
        return permissions;
    }
}
