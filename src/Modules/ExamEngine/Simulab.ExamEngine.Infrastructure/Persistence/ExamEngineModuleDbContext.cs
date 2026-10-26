using Microsoft.EntityFrameworkCore;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.ExamEngine.Infrastructure.Persistence.Configurations;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.ExamEngine.Infrastructure.Persistence;

/// <summary>
/// The ExamEngine module's context: its own schema, its own migrations (ADR-0001, decision 6). It holds the attempts
/// of students and the answers to their questions (F-166).
/// </summary>
public sealed class ExamEngineModuleDbContext(
    DbContextOptions<ExamEngineModuleDbContext> options,
    ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "exam_engine";

    protected override string Schema => SchemaName;

    /// <summary>F-166: the attempts, each owned by a student and fixed on one edition.</summary>
    public DbSet<Attempt> Attempts => Set<Attempt>();

    /// <summary>F-166: one row per question of an attempt, with the answer saved to it.</summary>
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AttemptAnswerConfiguration());
        modelBuilder.ApplyConfiguration(new AttemptConfiguration());
    }
}
