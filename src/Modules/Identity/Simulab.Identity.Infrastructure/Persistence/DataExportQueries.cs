using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// What Identity holds about one user (F-16 BR4), read with the module's filters on: an erased account is not found,
/// and a deleted role is not listed. Only the fields BR4 names are read, so a secret cannot slip in (BR5).
/// </summary>
public sealed class DataExportQueries(IdentityModuleDbContext context) : IDataExportQueries
{
    public async Task<IdentityDataResponse?> GetIdentityDataAsync(Guid userId, int activeSessions, CancellationToken cancellationToken = default)
    {
        var account = await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AccountDataResponse(
                user.Id,
                user.Email!,
                user.FullName,
                user.PhoneNumber,
                user.PreferredLanguage,
                user.CreatedAt,
                user.EmailVerifiedAt,
                user.IsAdultDeclared,
                user.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        var roles = await context.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Join(context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var consents = await context.ConsentRecords.AsNoTracking()
            .Where(consent => consent.UserId == userId)
            .OrderBy(consent => consent.AcceptedAt)
            .Select(consent => new ConsentDataResponse(
                consent.TermsVersion,
                consent.PrivacyVersion,
                consent.DeclaresAdult,
                consent.Locale,
                consent.AcceptedAt,
                consent.IpAddress))
            .ToListAsync(cancellationToken);

        var changes = await context.RoleChanges.AsNoTracking()
            .Where(change => change.TargetUserId == userId)
            .OrderBy(change => change.CreatedAt)
            .ThenBy(change => change.Id)
            .ToListAsync(cancellationToken);
        var roleChanges = changes
            .Select(change => new RoleChangeDataResponse(
                change.CreatedAt,
                change.Action.ToString(),
                change.RoleName,
                [.. change.Added.Select(item => item.Name)],
                [.. change.Removed.Select(item => item.Name)],
                change.CreatedBy == userId))
            .ToList();

        return new IdentityDataResponse(account, roles, consents, roleChanges, new SessionDataResponse(activeSessions));
    }
}
