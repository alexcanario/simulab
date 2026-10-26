using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Simulab.Persistence;

/// <summary>
/// The transaction a row lock needs (F-102, F-166). A <c>FOR SHARE</c> or <c>FOR UPDATE</c> read is only
/// worth anything while the transaction that took it is open, so the read opens one and the save that follows
/// commits it. A handler that refuses before saving leaves it open; the context rolls it back when the scope
/// ends, which releases the lock.
/// </summary>
public static class RowLockTransactions
{
    public static async Task BeginTransactionIfNoneAsync(this DatabaseFacade database, CancellationToken cancellationToken)
    {
        if (database.CurrentTransaction is null)
        {
            await database.BeginTransactionAsync(cancellationToken);
        }
    }

    public static async Task CommitTransactionIfAnyAsync(this DatabaseFacade database, CancellationToken cancellationToken)
    {
        if (database.CurrentTransaction is { } transaction)
        {
            await transaction.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
        }
    }

    public static async Task RollbackTransactionIfAnyAsync(this DatabaseFacade database, CancellationToken cancellationToken)
    {
        if (database.CurrentTransaction is { } transaction)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
        }
    }
}
