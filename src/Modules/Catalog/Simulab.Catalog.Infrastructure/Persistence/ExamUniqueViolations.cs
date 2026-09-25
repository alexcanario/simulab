using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate exam into the error the handler's own check gives (F-34 BR10,
/// the B-14 lesson). Only the exam's unique index is translated: any other database error is a real failure
/// and is left to propagate.
/// </summary>
public static class ExamUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public const string NameIndex = "ux_exams_tenant_authority_normalized_name";

    /// <summary>The 409 for a duplicate name inside the issuing authority, or null for anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName == NameIndex
            ? new Error(CatalogErrorCodes.ExamNameTaken, ErrorKind.Conflict)
            : null;
    }
}
