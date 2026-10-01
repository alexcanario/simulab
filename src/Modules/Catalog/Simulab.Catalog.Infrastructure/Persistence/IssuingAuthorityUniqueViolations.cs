using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate issuing authority into the error the handler's own check gives
/// (F-34 BR18 v2, the B-14 lesson). Only the name index is translated: any other database error is
/// a real failure and is left to propagate.
/// </summary>
public static class IssuingAuthorityUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public const string NameIndex = "ux_issuing_authorities_tenant_normalized_name";

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
            NameIndex => new Error(CatalogErrorCodes.IssuingAuthorityNameTaken, ErrorKind.Conflict),
            _ => null,
        };
    }
}
