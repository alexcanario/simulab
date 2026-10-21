using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Infrastructure.Persistence;

public sealed class ConsentRecordStore(IdentityModuleDbContext context) : IConsentRecordStore
{
    public async Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default)
    {
        await context.ConsentRecords.AddAsync(record, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    // F-85 BR1: the newest by time, then by id (a v7 Guid follows insertion); served by ix_consent_records_user_id.
    public Task<ConsentRecord?> FindLatestAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.ConsentRecords.AsNoTracking()
            .Where(record => record.UserId == userId)
            .OrderByDescending(record => record.AcceptedAt)
            .ThenByDescending(record => record.Id)
            .FirstOrDefaultAsync(cancellationToken);

    // F-85 BR4: same shape as RoleAdministrationStore.RunExclusiveAsync, one lock per user. The transaction-level
    // advisory lock goes with the commit or the rollback, so no lock outlives the request on a pooled connection.
    public async Task<Result<T>> RunExclusiveForUserAsync<T>(Guid userId, Func<Task<Result<T>>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var key = $"identity.consent:{userId}";
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);

        var result = await work();
        if (result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        return result;
    }
}
