using Microsoft.EntityFrameworkCore;
using Simulab.Ai.Persistence.Configurations;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Ai.Persistence;

/// <summary>
/// The gateway's own context: it owns the <c>ai</c> schema and the migration that creates the table
/// (F-41, BR5). Only the gateway writes through it, and the monthly count of BR3 reads from it.
/// </summary>
public sealed class AiDbContext(DbContextOptions<AiDbContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    /// <summary>The schema of the gateway. No module shares it.</summary>
    public const string SchemaName = "ai";

    /// <summary>The single table of the gateway.</summary>
    public const string TableName = "ai_calls";

    protected override string Schema => SchemaName;

    public DbSet<AiCall> Calls => Set<AiCall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AiCallConfiguration());
    }
}
