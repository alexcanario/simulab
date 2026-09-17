using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Security;

namespace Simulab.Persistence.Interceptors;

/// <summary>
/// Fills the audit fields and turns a delete into a soft delete before every save.
/// It works on the interfaces, so it also covers entities that cannot inherit <see cref="TenantEntity"/>
/// (the Identity user). Modules never set these fields by hand.
/// </summary>
public sealed class AuditAndSoftDeleteInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Deleted && entry.Entity is ISoftDeletableEntity softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = userId;
            }

            if (entry.Entity is not IAuditableEntity auditable)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userId;
                    auditable.UpdatedAt = null;
                    auditable.UpdatedBy = null;
                    break;
                case EntityState.Modified:
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userId;
                    break;
                default:
                    break;
            }
        }
    }
}
