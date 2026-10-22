using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Persistence;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-107: the blanks of the fill-in-the-blanks questions, one row per marker of the statement. The number is unique for
/// the question among the live rows (a removed blank is soft-deleted and keeps its number); the question reuses the row
/// of a number that stays, so a save never deletes and inserts the same number.
/// </summary>
public sealed class QuestionBlankConfiguration : IEntityTypeConfiguration<QuestionBlank>
{
    /// <summary>F-107 BR4: one blank per number in a question; the tenant column makes it NULLS NOT DISTINCT.</summary>
    public const string NumberIndex = "ux_question_blanks_tenant_question_number";

    /// <summary>F-107 BR3: a marker number is 1 to 99.</summary>
    public const string NumberCheck = "ck_question_blanks_number_range";

    public void Configure(EntityTypeBuilder<QuestionBlank> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_blanks",
            table =>
            {
                table.HasComment("One blank of a fill-in-the-blanks question: a marker [[n]] of the statement and the answers it accepts.");
                table.HasCheckConstraint(NumberCheck, $"number BETWEEN 1 AND {QuestionLimits.BlankNumberMax}");
            });
        builder.HasKey(blank => blank.Id);

        // The entity makes its own id, as an option does.
        builder.Property(blank => blank.Id).ValueGeneratedNever();

        builder.Property(blank => blank.QuestionId)
            .IsRequired()
            .HasComment("The fill-in-the-blanks question the blank belongs to.");

        builder.Property(blank => blank.Number)
            .IsRequired()
            .HasComment("The number of its marker [[n]] in the statement, 1 to 99; unique for the question among the live rows.");

        builder.Property(blank => blank.Position)
            .IsRequired()
            .HasComment("Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in.");

        builder.Property(blank => blank.AcceptedAnswers)
            .HasField("_acceptedAnswers")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired()
            .HasComment("The accepted answers, plain text, trimmed, in the order the curator wrote them. Empty only on a draft.");

        builder.HasIndex(blank => new { blank.TenantId, blank.QuestionId, blank.Number })
            .HasDatabaseName(NumberIndex)
            .IsUniquePerTenant()
            .HasFilter("is_deleted = false");

        // The loads filter by the question alone; the unique index cannot serve them because its first column is the tenant.
        builder.HasIndex(blank => blank.QuestionId)
            .HasDatabaseName("ix_question_blanks_question");
    }
}
