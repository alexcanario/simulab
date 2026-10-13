using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Infrastructure.Persistence.Configurations;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// Turns PostgreSQL's refusal of a repeated paper number into the error the handler's own check gives (F-96,
/// BR3): two saves racing for the same number both pass the pre-check and one meets the unique index. The same
/// goes for the number of a version (F-98, BR10): two saves that both read version N and both make N+1, and the
/// loser answers 409. Only those indexes are translated; any other database error is a real failure and propagates.
/// </summary>
public static class QuestionUniqueViolations
{
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    /// <summary>The 409 for a repeated number inside an edition or a repeated version number, or null for anything else.</summary>
    public static Error? Translate(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is not PostgresException { SqlState: UniqueViolation } violation)
        {
            return null;
        }

        return violation.ConstraintName switch
        {
            QuestionConfiguration.PaperNumberIndex =>
                new Error(QuestionBankErrorCodes.QuestionPaperNumberTaken, ErrorKind.Conflict),
            QuestionVersionConfiguration.NumberIndex =>
                new Error(QuestionBankErrorCodes.QuestionVersionConflict, ErrorKind.Conflict),
            _ => null
        };
    }
}
