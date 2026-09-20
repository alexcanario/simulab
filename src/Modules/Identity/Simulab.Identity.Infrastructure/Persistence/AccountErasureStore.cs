using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// Erasing an account (F-10, BR4, BR8, BR9). Everything here runs inside the transaction opened by
/// <see cref="IRoleAdministrationStore.RunExclusiveAsync{T}"/>, so a failure anywhere leaves the account
/// exactly as it was.
/// </summary>
public sealed class AccountErasureStore(IdentityModuleDbContext context, ILookupNormalizer normalizer) : IAccountErasureStore
{
    public Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public void ApplyErasure(User user, string tombstoneAddress)
    {
        ArgumentNullException.ThrowIfNull(user);

        // The unique indexes are over the normalized columns, so the tombstone has to reach them too:
        // without this the old address would stay reserved (the real trap behind BR5).
        user.NormalizedEmail = normalizer.NormalizeEmail(tombstoneAddress);
        user.NormalizedUserName = normalizer.NormalizeName(tombstoneAddress);

        // BR6: the interceptor turns the remove into IsDeleted, DeletedAt and DeletedBy.
        context.Users.Remove(user);
    }

    public async Task RemoveAccountDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // A hard delete, on purpose: these rows exist only to serve this account and nothing counts them.
        // Query filters are ignored so a token that was already soft deleted goes as well.
        await context.EmailVerificationTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.UserRoles.Where(userRole => userRole.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.UserClaims.Where(claim => claim.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.UserLogins.Where(login => login.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.UserTokens.Where(token => token.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        // The protocol tables keep the subject as text. Tokens first: they point at the authorizations.
        var subject = userId.ToString();
        await context.Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(token => token.Subject == subject)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => authorization.Subject == subject)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// BR9: one statement, because the record has no other reason to be loaded and is written once
    /// everywhere else. The soft-delete filter is ignored so no consent evidence keeps an address.
    /// </summary>
    public Task ClearConsentAddressesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.ConsentRecords
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .Where(record => record.UserId == userId && record.IpAddress != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(record => record.IpAddress, (string?)null), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
