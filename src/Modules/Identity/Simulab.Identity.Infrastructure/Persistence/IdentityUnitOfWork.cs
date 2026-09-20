using Simulab.Identity.Application.Abstractions;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>The module's unit of work is its own context; the job table is mapped into it (F-13 BR2).</summary>
public sealed class IdentityUnitOfWork(IdentityModuleDbContext context) : IIdentityUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
