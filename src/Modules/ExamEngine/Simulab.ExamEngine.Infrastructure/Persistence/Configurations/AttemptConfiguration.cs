using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.ExamEngine.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-166: the attempts of students. The student and the edition are held by id, never by a foreign key (they belong to
/// Identity and Catalog). One attempt in progress per student and edition is a partial unique index (BR3); a submitted
/// attempt has its time and an attempt in progress has none, in the database as in the domain (BR6).
/// </summary>
public sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    /// <summary>BR3: the unique rule of one attempt in progress per student and edition.</summary>
    public const string InProgressIndex = "ux_attempts_tenant_student_edition_in_progress";

    public const string SubmittedAtCheck = "ck_attempts_status_submitted_at";

    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "attempts",
            table =>
            {
                table.HasComment("A student's attempt at a published exam edition: the questions it fixed at the start and the answers saved so far. One in progress per student and edition; a submitted attempt never changes.");
                table.HasCheckConstraint(
                    SubmittedAtCheck,
                    "(status = 'Submitted' AND submitted_at IS NOT NULL) OR (status = 'InProgress' AND submitted_at IS NULL)");
            });
        builder.HasKey(attempt => attempt.Id);

        // The entity makes its own id, as the questions do.
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();

        builder.Property(attempt => attempt.StudentId)
            .IsRequired()
            .HasComment("The student who owns the attempt, an Identity user id. No foreign key: the user lives in another module.");

        builder.Property(attempt => attempt.ExamEditionId)
            .IsRequired()
            .HasComment("The published exam edition taken, a Catalog edition id. No foreign key: the edition lives in another module.");

        builder.Property(attempt => attempt.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Where the attempt is: InProgress takes answers, Submitted never changes again.");

        builder.Property(attempt => attempt.StartedAt)
            .IsRequired()
            .HasComment("When the attempt started, from the server clock.");

        builder.Property(attempt => attempt.SubmittedAt)
            .HasComment("When the student submitted, from the server clock. Null while the attempt is in progress.");

        builder.HasMany(attempt => attempt.Answers)
            .WithOne()
            .HasForeignKey(answer => answer.AttemptId)
            .HasConstraintName("fk_attempt_answers_attempt")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(attempt => attempt.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);

        // BR3: at most one attempt in progress per student and edition; the tenant column makes it NULLS NOT DISTINCT.
        builder.HasIndex(attempt => new { attempt.TenantId, attempt.StudentId, attempt.ExamEditionId })
            .HasDatabaseName(InProgressIndex)
            .HasFilter($"status = '{nameof(AttemptStatus.InProgress)}' AND is_deleted = false")
            .IsUniquePerTenant();

        // F-167 BR6: every attempt of a student, for the erasure of the texts and the data export.
        builder.HasIndex(attempt => attempt.StudentId).HasDatabaseName("ix_attempts_student");
    }
}
