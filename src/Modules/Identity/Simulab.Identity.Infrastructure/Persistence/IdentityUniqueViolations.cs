using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Identity.Application.Abstractions;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a duplicate the sign-up handlers' own lookup missed into the answer that
/// lookup would have given (F-30 BR6), the way <c>OrganizerUniqueViolations</c> does it for the Catalog
/// (B-14). Only the two indexes a sign-up can hit are translated: any other database error is a real
/// failure and is left to propagate (AC8c).
/// </summary>
internal static class IdentityUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public const string UserEmailIndex = "ux_users_tenant_normalized_email";
    public const string GoogleLoginIndex = "pk_user_logins";

    public static IdentityUniqueViolation? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName switch
        {
            UserEmailIndex => IdentityUniqueViolation.UserEmail,
            GoogleLoginIndex => IdentityUniqueViolation.GoogleLogin,
            _ => null,
        };
    }
}
