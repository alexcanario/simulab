using Microsoft.EntityFrameworkCore;
using Simulab.Jobs.Persistence.Configurations;

namespace Simulab.Jobs.Persistence;

public static class JobModelBuilderExtensions
{
    /// <summary>
    /// Maps the job table into a module's own <c>DbContext</c> so a handler can stage a job in the very
    /// <c>SaveChanges</c> that writes the data justifying it (F-13 BR2). The table itself belongs to
    /// <see cref="JobsDbContext"/>: it is excluded from this context's migrations, so the module's
    /// migration never tries to create it a second time.
    /// </summary>
    public static ModelBuilder AddJobQueue(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.Entity<Job>().ToTable(
            JobsDbContext.TableName,
            JobsDbContext.SchemaName,
            table => table.ExcludeFromMigrations());

        return modelBuilder;
    }
}
