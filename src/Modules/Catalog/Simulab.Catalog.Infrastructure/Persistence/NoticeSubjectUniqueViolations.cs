using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate notice subject into the error the handler's own check gives
/// (F-74 BR5, the B-14 lesson). Only the label index is translated: any other database error is a real failure.
/// </summary>
public static class NoticeSubjectUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    /// <summary>The 409 for a duplicate label inside the group of the edition, or null for anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        // Two saves of one row at once can both add the same entry (F-75): the second is a conflict, not a 500.
        return violation.ConstraintName switch
        {
            NoticeSubjectConfiguration.UniqueIndex => new Error(CatalogErrorCodes.NoticeSubjectDuplicate, ErrorKind.Conflict),
            NoticeSubjectMappingConfiguration.UniqueIndex => new Error(CatalogErrorCodes.NoticeSubjectMappingConflict, ErrorKind.Conflict),
            _ => null
        };
    }
}
