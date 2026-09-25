using Microsoft.EntityFrameworkCore;
using Simulab.Persistence.Conventions;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Security;

namespace Simulab.Persistence;

/// <summary>
/// Base for a module's own <see cref="DbContext"/>: one schema and one context per module
/// (ADR-0001, decision 6). It carries only what every module repeats — schema and migrations
/// history table, the tenant and soft-delete query filters, and the audit interceptor through
/// <see cref="PersistenceServiceCollectionExtensions"/>. Entities, configurations and migrations
/// stay in the module.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options, ICurrentTenant currentTenant) : DbContext(options)
{
    /// <summary>Filter names, so a query can ignore one of them alone.</summary>
    public const string TenantFilter = "tenant";

    /// <summary>Filter name for the soft-delete filter.</summary>
    public const string SoftDeleteFilter = "soft_delete";

    /// <summary>The module's PostgreSQL schema. One module, one schema; two modules never share it.</summary>
    protected abstract string Schema { get; }

    /// <summary>The tenant of the current request; null means global and B2C data only (v1).</summary>
    protected Guid? CurrentTenantId => currentTenant.TenantId;

    /// <summary>
    /// The second thing every module repeats, beside the filters below: the descriptions of the audit,
    /// tenant and soft-delete columns (F-24, BR3). A convention and not a loop in
    /// <see cref="OnModelCreating"/> because a module can add entity types after calling the base, and
    /// the same table would then be described in one context and not in another.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new StandardColumnCommentConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(TenantEntity)))
            {
                ApplyFilters(modelBuilder, entityType.ClrType);
            }
        }
    }

    private void ApplyFilters(ModelBuilder modelBuilder, Type clrType) =>
        typeof(ModuleDbContext)
            .GetMethod(nameof(ApplyFiltersTo), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(clrType)
            .Invoke(this, [modelBuilder]);

    // The expressions capture `this`, not the tenant value: EF's compiled model is shared between
    // instances, and a captured value would leak one tenant's rows into the next request (Simulae TK #184).
    private void ApplyFiltersTo<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantEntity =>
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(TenantFilter, entity => entity.TenantId == null || entity.TenantId == CurrentTenantId)
            .HasQueryFilter(SoftDeleteFilter, entity => !entity.IsDeleted);
}
