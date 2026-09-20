using Microsoft.EntityFrameworkCore;
using Simulab.Jobs.Persistence.Configurations;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Jobs.Persistence;

/// <summary>
/// The queue's own context: it owns the <c>jobs</c> schema and the migration that creates the table
/// (F-13 BR1). Only the worker reads through it; a module stages its jobs through its own context
/// (<see cref="JobModelBuilderExtensions.AddJobQueue"/>) so the job and the data that justifies it
/// share one transaction (BR2).
/// </summary>
public sealed class JobsDbContext(DbContextOptions<JobsDbContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    /// <summary>The schema of the queue. No module shares it.</summary>
    public const string SchemaName = "jobs";

    /// <summary>The single table of the queue.</summary>
    public const string TableName = "jobs";

    /// <summary>Schema and table, as raw SQL has to spell them.</summary>
    public const string QualifiedTableName = $"{SchemaName}.{TableName}";

    protected override string Schema => SchemaName;

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new JobConfiguration());
    }
}
