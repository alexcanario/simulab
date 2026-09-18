using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence;

public sealed class ConsentRecordStore(IdentityModuleDbContext context) : IConsentRecordStore
{
    public async Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default)
    {
        await context.ConsentRecords.AddAsync(record, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
