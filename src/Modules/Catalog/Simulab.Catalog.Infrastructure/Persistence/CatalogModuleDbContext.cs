using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The Catalog module's context: its own schema, its own migrations (ADR-0001, decision 6). It holds
/// the assessment catalog — organizers (F-33) and exams (F-34); editions next.
/// </summary>
public sealed class CatalogModuleDbContext(DbContextOptions<CatalogModuleDbContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "catalog";

    protected override string Schema => SchemaName;

    /// <summary>F-33: who runs an exam.</summary>
    public DbSet<Organizer> Organizers => Set<Organizer>();

    /// <summary>F-34: the exams, each under the body that publishes its notice.</summary>
    public DbSet<Exam> Exams => Set<Exam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrganizerConfiguration());
        modelBuilder.ApplyConfiguration(new ExamConfiguration());
    }
}
