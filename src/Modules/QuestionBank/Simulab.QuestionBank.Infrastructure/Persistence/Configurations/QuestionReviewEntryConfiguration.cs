using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-105: the append-only history of a question's review, one row per send, withdrawal, approval or rejection
/// (BR7). The key to the question stays inside the module and restricts a delete: a question is only ever
/// soft-deleted and an entry is never removed.
/// </summary>
public sealed class QuestionReviewEntryConfiguration : IEntityTypeConfiguration<QuestionReviewEntry>
{
    public const string QuestionCreatedAtIndex = "ix_question_review_entries_question_id_created_at";

    public void Configure(EntityTypeBuilder<QuestionReviewEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_review_entries",
            table => table.HasComment("One event in the review of a question: sent, withdrawn, approved or rejected, with the reason or note. Append-only: never edited or deleted."));
        builder.HasKey(entry => entry.Id);

        // The entity makes its own id, as the question and its versions do.
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.QuestionId)
            .IsRequired()
            .HasComment("The question the entry belongs to.");

        builder.Property(entry => entry.Kind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("What happened: Submitted, Withdrawn, Approved or Rejected.");

        builder.Property(entry => entry.Reason)
            .HasMaxLength(QuestionLimits.ChangeReasonMaxLength)
            .HasComment("The reason of a rejection or the note of an approval, trimmed. Null when none was given.");

        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(entry => entry.QuestionId)
            .HasConstraintName("fk_question_review_entries_question")
            .OnDelete(DeleteBehavior.Restrict);

        // The history of one question and the time of its latest send read by the question, oldest first.
        builder.HasIndex(entry => new { entry.QuestionId, entry.CreatedAt })
            .HasDatabaseName(QuestionCreatedAtIndex);
    }
}
