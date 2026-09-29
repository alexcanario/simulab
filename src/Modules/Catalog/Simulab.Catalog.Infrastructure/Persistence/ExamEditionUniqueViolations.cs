using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate edition into the error the handler's own check gives (F-35
/// BR10, the B-14 lesson). Only the edition's unique index is translated: any other database error is a
/// real failure and is left to propagate.
/// </summary>
public static class ExamEditionUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    /// <summary>The 409 for a duplicate year, position and board inside the exam, or null for anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName == ExamEditionConfiguration.UniqueIndex
            ? new Error(CatalogErrorCodes.ExamEditionDuplicate, ErrorKind.Conflict)
            : null;
    }
}
