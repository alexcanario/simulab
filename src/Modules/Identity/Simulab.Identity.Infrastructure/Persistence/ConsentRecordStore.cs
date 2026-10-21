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

    public Task<ConsentRecord?> FindLatestAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Result<T>> RunExclusiveForUserAsync<T>(Guid userId, Func<Task<Result<T>>> work, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
