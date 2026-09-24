using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// Removes an organizer from the catalog (F-33 UC4). The removal is a soft delete (F-33 BR11): the row
/// stays, so its name and acronym stay taken. Since F-34 an organizer may be an exam's issuing authority,
/// and then it does not leave the catalog at all (F-34 BR12): cascading would let one click remove a whole
/// catalog, and leaving the exams orphaned contradicts the required link.
/// </summary>
public sealed class DeleteOrganizerHandler(IOrganizerStore store, IExamStore exams)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizer = await store.FindAsync(id, cancellationToken);
        if (organizer is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerNotFound, ErrorKind.NotFound));
        }

        // F-34 BR12. Deleted exams do not count: they are gone from the catalog, so nothing is orphaned.
        if (await exams.OrganizerHasExamsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.OrganizerHasExams, ErrorKind.Conflict));
        }

        store.Remove(organizer);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
