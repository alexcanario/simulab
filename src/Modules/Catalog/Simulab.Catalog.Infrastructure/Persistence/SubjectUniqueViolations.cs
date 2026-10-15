using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate subject into the error the handler's own check gives (F-79 BR5,
/// the B-14 lesson). Only the name index is translated: any other database error is a real failure.
/// </summary>
public static class SubjectUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public const string NameIndex = "ux_subjects_tenant_normalized_name";

    /// <summary>F-77: an other name is unique across every subject, live rows only.</summary>
    public const string AliasIndex = "ux_subject_aliases_tenant_normalized_name";

    /// <summary>The 409 for a duplicate name, or null when the exception is anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName switch
        {
            NameIndex => new Error(CatalogErrorCodes.SubjectNameTaken, ErrorKind.Conflict),
            AliasIndex => new Error(CatalogErrorCodes.SubjectAliasTaken, ErrorKind.Conflict),
            _ => null,
        };
    }
}
