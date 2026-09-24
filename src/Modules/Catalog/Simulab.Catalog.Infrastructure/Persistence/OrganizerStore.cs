using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Organizers;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The organizer port over the module's context (F-33). The two "is taken" queries ignore the
/// soft-delete filter on purpose: a deleted organizer keeps its name and acronym, exactly as the
/// unique indexes see them (BR9, BR11).
/// </summary>
public sealed class OrganizerStore(CatalogModuleDbContext context) : IOrganizerStore
{
    public Task<Organizer?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Organizers.FirstOrDefaultAsync(organizer => organizer.Id == id, cancellationToken);

    public Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(organizer => organizer.NormalizedName == normalizedName, cancellationToken);

    public Task<bool> AcronymIsTakenAsync(string normalizedAcronym, Guid? exceptId, CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(organizer => organizer.NormalizedAcronym == normalizedAcronym, cancellationToken);

    public void Add(Organizer organizer) => context.Organizers.Add(organizer);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE (BR11).
    public void Remove(Organizer organizer) => context.Organizers.Remove(organizer);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (OrganizerUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked as added; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();

            return OrganizerUniqueViolations.Translate(exception);
        }
    }

    // Every organizer but the one being edited, deleted ones included. The exclusion is added as its own
    // Where instead of `Id != exceptId`: a null parameter in a comparison is SQL's three-valued logic,
    // and the row the caller is editing would be the one it wrongly keeps or drops.
    private IQueryable<Organizer> Others(Guid? exceptId)
    {
        var query = context.Organizers.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(organizer => organizer.Id != id) : query;
    }
}
