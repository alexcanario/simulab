using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// Looks an account up before there is a session. The tenant filter is ignored on purpose (see
/// <see cref="IUserDirectory"/>); the soft-delete filter is not, so an erased account is never found.
/// That alone does not free the address — the unique index does not read <c>IsDeleted</c> — which is why
/// erasure overwrites the address columns themselves (F-10, BR4, BR5).
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

    public Task<User?> FindByLoginIgnoringTenantAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default) =>
        context.Users
            .IgnoreQueryFilters([ModuleDbContext.TenantFilter])
            .Where(user => context.UserLogins.Any(login =>
                login.UserId == user.Id && login.LoginProvider == loginProvider && login.ProviderKey == providerKey))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<string?> FindLoginKeyAsync(Guid userId, string loginProvider, CancellationToken cancellationToken = default) =>
        context.UserLogins
            .Where(login => login.UserId == userId && login.LoginProvider == loginProvider)
            .Select(login => login.ProviderKey)
            .FirstOrDefaultAsync(cancellationToken);
}
