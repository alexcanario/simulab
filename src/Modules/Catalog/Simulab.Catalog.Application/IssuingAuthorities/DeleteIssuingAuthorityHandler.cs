using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.IssuingAuthorities;

/// <summary>
/// Removes an issuing authority from the catalog (F-34 BR12, v2). The removal is a soft delete: the row
/// stays, so its name and acronym stay taken. A body that still has exams does not leave at all — cascading
/// would let one click remove a whole catalog, and leaving the exams orphaned contradicts the required link.
/// </summary>
public sealed class DeleteIssuingAuthorityHandler(IIssuingAuthorityStore store, IExamStore exams)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var authority = await store.FindAsync(id, cancellationToken);
        if (authority is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityNotFound, ErrorKind.NotFound));
        }

        // Deleted exams do not count: they are gone from the catalog, so nothing is orphaned.
        if (await exams.IssuingAuthorityHasExamsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.IssuingAuthorityHasExams, ErrorKind.Conflict));
        }

        store.Remove(authority);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
