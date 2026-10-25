using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Persistence;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-109: the criteria of the rubric of the open-answer and essay questions, one row per criterion. The position is unique
/// for the question among the live rows (a removed criterion is soft-deleted and keeps its position); the question reuses
/// the row of a position that stays, so a save never deletes and inserts the same position. Names stay unique in the
/// domain: an index cannot express "ignoring case and extra white space" cheaply.
/// </summary>
public sealed class QuestionRubricCriterionConfiguration : IEntityTypeConfiguration<QuestionRubricCriterion>
{
    /// <summary>F-109 BR3: one criterion per position in a question; the tenant column makes it NULLS NOT DISTINCT.</summary>
    public const string PositionIndex = "ux_question_rubric_criteria_tenant_question_position";

    /// <summary>F-109 BR3: the points of a criterion are 1 to 100.</summary>
    public const string PointsCheck = "ck_question_rubric_criteria_points_range";

    public void Configure(EntityTypeBuilder<QuestionRubricCriterion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_rubric_criteria",
            table =>
            {
                table.HasComment("One criterion of the rubric of an open-answer or essay question: what a grader scores and the most points it can give. The question's grade is the points given divided by the total of the maximums.");
                table.HasCheckConstraint(
                    PointsCheck,
                    $"max_points BETWEEN {QuestionLimits.CriterionPointsMin} AND {QuestionLimits.CriterionPointsMax}");
            });
        builder.HasKey(criterion => criterion.Id);

        // The entity makes its own id, as an option does.
        builder.Property(criterion => criterion.Id).ValueGeneratedNever();

        builder.Property(criterion => criterion.QuestionId)
            .IsRequired()
            .HasComment("The open-answer or essay question the criterion belongs to.");

        builder.Property(criterion => criterion.Position)
            .IsRequired()
            .HasComment("Where the criterion sits in the rubric, from 1: the order the curator wrote; unique for the question among the live rows.");

        builder.Property(criterion => criterion.Name)
            .HasMaxLength(QuestionLimits.CriterionNameMaxLength)
            .IsRequired()
            .HasComment("The name of the criterion, plain text, trimmed; unique in the rubric ignoring case and extra white space.");

        builder.Property(criterion => criterion.Description)
            .HasMaxLength(QuestionLimits.CriterionDescriptionMaxLength)
            .HasComment("What the grader looks for, plain text, trimmed. Empty when there is none.");

        builder.Property(criterion => criterion.MaxPoints)
            .IsRequired()
            .HasComment("The most points the criterion gives, a whole number from 1 to 100.");

        builder.HasIndex(criterion => new { criterion.TenantId, criterion.QuestionId, criterion.Position })
            .HasDatabaseName(PositionIndex)
            .IsUniquePerTenant()
            .HasFilter("is_deleted = false");

        // The loads filter by the question alone; the unique index cannot serve them because its first column is the tenant.
        builder.HasIndex(criterion => criterion.QuestionId)
            .HasDatabaseName("ix_question_rubric_criteria_question");
    }
}
