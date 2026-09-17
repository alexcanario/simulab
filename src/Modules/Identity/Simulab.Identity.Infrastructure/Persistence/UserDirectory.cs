using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// Looks an account up before there is a session. The tenant filter is ignored on purpose (see
/// <see cref="IUserDirectory"/>); the soft-delete filter is not, so an erased account frees its address.
/// </summary>
public sealed class UserDirectory(IdentityModuleDbContext context, ILookupNormalizer normalizer) : IUserDirectory
{
    public Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = normalizer.NormalizeEmail(email);

        return context.Users
            .IgnoreQueryFilters([ModuleDbContext.TenantFilter])
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalized, cancellationToken);
    }
}
