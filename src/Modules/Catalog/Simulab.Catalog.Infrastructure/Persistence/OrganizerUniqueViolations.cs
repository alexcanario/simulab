using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate organizer into the error the handler's own check gives (B-14,
/// F-33 BR9). Only the two organizer unique indexes are translated: any other database error is a real
/// failure and is left to propagate.
/// </summary>
public static class OrganizerUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public const string NameIndex = "ux_organizers_tenant_normalized_name";
    public const string AcronymIndex = "ux_organizers_tenant_normalized_acronym";

    /// <summary>The 409 for a duplicate name or acronym, or null when the exception is anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName switch
        {
            NameIndex => new Error(CatalogErrorCodes.OrganizerNameTaken, ErrorKind.Conflict),
            AcronymIndex => new Error(CatalogErrorCodes.OrganizerAcronymTaken, ErrorKind.Conflict),
            _ => null,
        };
    }
}
