using Microsoft.EntityFrameworkCore.Storage;
using Simulab.Identity.Application.Abstractions;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// Commits <paramref name="transaction"/> on <see cref="CommitAsync"/>; disposing before that rolls it
/// back and clears <paramref name="context"/>'s change tracker (F-30 decision), so nothing staged before
/// the rollback survives into a later write of the same request (BR3, AC9).
/// </summary>
internal sealed class IdentityTransaction(IDbContextTransaction transaction, IdentityModuleDbContext context) : IIdentityTransaction
{
    private bool _committed;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
        }

        await transaction.DisposeAsync();
    }
}
