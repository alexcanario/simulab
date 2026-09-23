using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// Removes an organizer from the catalog (UC4). The removal is a soft delete (BR11): the row stays,
/// so its name and acronym stay taken. Nothing references an organizer yet — the guard for an
/// organizer that has exams arrives with F-34.
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
