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

    /// <summary>F-33: the board that elaborates, applies and marks a paper.</summary>
    public DbSet<Organizer> Organizers => Set<Organizer>();

    /// <summary>F-34 v2: the body that publishes a notice, and that every exam hangs on.</summary>
    public DbSet<IssuingAuthority> IssuingAuthorities => Set<IssuingAuthority>();

    /// <summary>F-34: the exams, each under the body that publishes its notice.</summary>
    public DbSet<Exam> Exams => Set<Exam>();

    /// <summary>F-35: the papers actually applied, each under its exam and naming its board.</summary>
    public DbSet<ExamEdition> ExamEditions => Set<ExamEdition>();

    /// <summary>F-79: the fixed list of areas a subject may belong to, seeded by a migration.</summary>
    public DbSet<Area> Areas => Set<Area>();

    /// <summary>F-79: the subjects of the canonical taxonomy.</summary>
    public DbSet<Subject> Subjects => Set<Subject>();

    /// <summary>F-79: the topics, each under its subject.</summary>
    public DbSet<Topic> Topics => Set<Topic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrganizerConfiguration());
        modelBuilder.ApplyConfiguration(new IssuingAuthorityConfiguration());
        modelBuilder.ApplyConfiguration(new ExamConfiguration());
        modelBuilder.ApplyConfiguration(new ExamEditionConfiguration());
        modelBuilder.ApplyConfiguration(new AreaConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectConfiguration());
        modelBuilder.ApplyConfiguration(new TopicConfiguration());
    }
}
