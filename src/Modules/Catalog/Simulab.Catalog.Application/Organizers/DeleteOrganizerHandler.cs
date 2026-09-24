using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// Removes an organizer from the catalog (F-33 UC4). The removal is a soft delete (F-33 BR11): the row
/// stays, so its name and acronym stay taken. Nothing points at an organizer yet — F-34 v2 moved the guard
/// to the issuing authority, which is what an exam hangs on; the board gets its own when F-35's editions
/// start pointing here.
/// </summary>
public sealed class DeleteOrganizerHandler(IOrganizerStore store)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizer = await store.FindAsync(id, cancellationToken);
        if (organizer is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerNotFound, ErrorKind.NotFound));
        }

        store.Remove(organizer);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
