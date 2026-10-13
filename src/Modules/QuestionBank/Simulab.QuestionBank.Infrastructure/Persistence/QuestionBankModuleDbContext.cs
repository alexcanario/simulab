using Microsoft.EntityFrameworkCore;
using Simulab.Persistence;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.QuestionBank.Infrastructure.Persistence.Configurations;
using Simulab.SharedKernel.Security;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The QuestionBank module's context: its own schema, its own migrations (ADR-0001, decision 6; ADR-0005).
/// It holds the questions and their options (F-95) and the versions of the published ones (F-98).
/// </summary>
public sealed class QuestionBankModuleDbContext(
    DbContextOptions<QuestionBankModuleDbContext> options,
    ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "question_bank";

    protected override string Schema => SchemaName;

    /// <summary>F-95: the questions, each pointing at a catalog topic by id.</summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>F-95: the options of the questions, in position order.</summary>
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    /// <summary>F-98: the immutable snapshots of the published questions, numbered per question.</summary>
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionOptionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionVersionConfiguration());
    }
}
