using Microsoft.EntityFrameworkCore;
using Simulab.SharedKernel.Security;

namespace Simulab.Persistence.Tests;

/// <summary>A module context, exactly as a module declares one: schema, sets, configurations.</summary>
public sealed class SampleContext(DbContextOptions<SampleContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "sample";

    protected override string Schema => SchemaName;

    public DbSet<SampleItem> Items => Set<SampleItem>();

    public DbSet<SampleNote> Notes => Set<SampleNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SampleItem>()
            .HasIndex(item => new { item.TenantId, item.Name })
            .IsUniquePerTenant();
    }
}
