using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-95: the questions table. A question points at its topic by id and has no foreign key to the catalog's
/// table: a module never touches another module's tables, and the guard that keeps a topic from being deleted
/// while a question uses it is a check the catalog asks (BR13), not a constraint. The two indexes serve the
/// guard and the list.
/// </summary>
public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public const string DifficultyCheck = "ck_questions_difficulty_range";

    public void Configure(EntityTypeBuilder<Question> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "questions",
            table =>
            {
                table.HasComment("A question of the bank, written by a curator. Its options are in question_options.");
                table.HasCheckConstraint(
                    DifficultyCheck,
                    $"difficulty IS NULL OR difficulty BETWEEN {QuestionLimits.DifficultyMin} AND {QuestionLimits.DifficultyMax}");
            });
        builder.HasKey(question => question.Id);

        builder.Property(question => question.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("How the question is answered: SingleChoice.");

        builder.Property(question => question.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("Where the question is in its life: Draft or Published.");

        builder.Property(question => question.TopicId)
            .IsRequired()
            .HasComment("The catalog topic the question belongs to. Held by id; there is no foreign key to the catalog.");

        builder.Property(question => question.ContentLanguage)
            .HasMaxLength(QuestionLimits.ContentLanguageMaxLength)
            .IsRequired()
            .HasComment("The language the question is written in, such as pt-BR. Question content is never translated.");

        builder.Property(question => question.StatementHtml)
            .HasMaxLength(QuestionLimits.StatementMaxLength)
            .IsRequired()
            .HasComment("The statement as sanitized HTML.");

        builder.Property(question => question.StatementText)
            .HasMaxLength(QuestionLimits.StatementMaxLength)
            .IsRequired()
            .HasComment("The statement without tags. The list excerpt reads it; derived on every write.");

        builder.Property(question => question.NormalizedStatement)
            .HasMaxLength(QuestionLimits.StatementMaxLength)
            .IsRequired()
            .HasComment("The plain statement without case or accents. The list search reads it; derived on every write.");

        builder.Property(question => question.BaseTextHtml)
            .HasMaxLength(QuestionLimits.BaseTextMaxLength)
            .HasComment("The text the question refers to, as sanitized HTML. Empty when there is none.");

        builder.Property(question => question.ExplanationHtml)
            .HasMaxLength(QuestionLimits.ExplanationMaxLength)
            .HasComment("Why the key is right, as sanitized HTML. Empty when there is none.");

        builder.Property(question => question.Difficulty)
            .HasComment("From 1 (easiest) to 5 (hardest). Empty when not set.");

        // Skeleton of the test phase (F-96): not mapped until the migration lands.
        builder.Ignore(question => question.ExamEditionId);
        builder.Ignore(question => question.PaperNumber);
        builder.Ignore(question => question.NoticeSubjectId);

        builder.HasMany(question => question.Options)
            .WithOne()
            .HasForeignKey(option => option.QuestionId)
            .HasConstraintName("fk_question_options_question")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(question => question.Options).UsePropertyAccessMode(PropertyAccessMode.Field);

        // The topic delete guard and the topic's "in use" flag filter by the topic alone.
        builder.HasIndex(question => question.TopicId)
            .HasDatabaseName("ix_questions_topic");

        // The list: a status filter, newest first.
        builder.HasIndex(question => new { question.Status, question.CreatedAt })
            .HasDatabaseName("ix_questions_status_created_at");
    }
}
