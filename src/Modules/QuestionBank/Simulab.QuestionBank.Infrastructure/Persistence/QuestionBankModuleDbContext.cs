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

    /// <summary>F-106: the left items of the matching questions, in position order.</summary>
    public DbSet<QuestionMatchItem> QuestionMatchItems => Set<QuestionMatchItem>();

    /// <summary>F-107: the blanks of the fill-in-the-blanks questions, one per marker.</summary>
    public DbSet<QuestionBlank> QuestionBlanks => Set<QuestionBlank>();

    /// <summary>F-109: the criteria of the rubric of the open-answer and essay questions, in position order.</summary>
    public DbSet<QuestionRubricCriterion> QuestionRubricCriteria => Set<QuestionRubricCriterion>();

    /// <summary>F-103: the pictures of the statements, options and base texts; the files are in the storage.</summary>
    public DbSet<QuestionImage> QuestionImages => Set<QuestionImage>();

    /// <summary>F-98: the immutable snapshots of the published questions, numbered per question.</summary>
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();

    /// <summary>F-102: the passages several questions share, linked through <c>questions.base_text_id</c>.</summary>
    public DbSet<BaseText> BaseTexts => Set<BaseText>();

    /// <summary>F-105: the append-only history of each question's review.</summary>
    public DbSet<QuestionReviewEntry> QuestionReviewEntries => Set<QuestionReviewEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BaseTextConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionBlankConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionImageConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionMatchItemConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionOptionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionReviewEntryConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionRubricCriterionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionVersionConfiguration());
    }
}
