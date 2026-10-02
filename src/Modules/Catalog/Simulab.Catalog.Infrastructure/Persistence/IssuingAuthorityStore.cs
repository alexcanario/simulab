using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.IssuingAuthorities;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The issuing-authority port over the module's context (F-34 BR18, v2). The "is taken" query ignore
/// the soft-delete filter on purpose: a deleted body keeps its name, exactly as the unique
/// indexes see them.
/// </summary>
public sealed class IssuingAuthorityStore(CatalogModuleDbContext context) : IIssuingAuthorityStore
{
    public Task<IssuingAuthority?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.IssuingAuthorities.FirstOrDefaultAsync(authority => authority.Id == id, cancellationToken);

    public Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(authority => authority.NormalizedName == normalizedName, cancellationToken);

    public void Add(IssuingAuthority authority) => context.IssuingAuthorities.Add(authority);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE.
    public void Remove(IssuingAuthority authority) => context.IssuingAuthorities.Remove(authority);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (IssuingAuthorityUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked as added; drop it so a later save does not retry it.
            context.ChangeTracker.Clear();

            return IssuingAuthorityUniqueViolations.Translate(exception);
        }
    }

    // Every body but the one being edited, deleted ones included. The exclusion is its own Where instead of
    // `Id != exceptId`: a null parameter in a comparison is SQL's three-valued logic (the F-33 lesson).
    private IQueryable<IssuingAuthority> Others(Guid? exceptId)
    {
        var query = context.IssuingAuthorities.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(authority => authority.Id != id) : query;
    }
}
