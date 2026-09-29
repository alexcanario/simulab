using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// Removes an organizer from the catalog (F-33 UC4). The removal is a soft delete (F-33 BR11): the row
/// stays, so its name and acronym stay taken. A board that some edition names does not leave at all
/// (F-35 BR12): the edition would point at nothing.
/// </summary>
public sealed class DeleteOrganizerHandler(IOrganizerStore store, IExamEditionStore editions)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizer = await store.FindAsync(id, cancellationToken);
        if (organizer is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerNotFound, ErrorKind.NotFound));
        }

        // Deleted editions do not count: they are gone from the catalog, so nothing is orphaned.
        if (await editions.OrganizerHasEditionsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerHasEditions, ErrorKind.Conflict));
        }

        store.Remove(organizer);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
