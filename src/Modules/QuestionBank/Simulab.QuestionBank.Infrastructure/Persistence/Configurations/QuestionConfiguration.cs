using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Persistence;
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

    /// <summary>F-96: the three origin columns are all null, or the edition and the number are both set.</summary>
    public const string OriginCheck = "ck_questions_origin";

    /// <summary>F-96 BR3: the unique number of a live question inside an edition.</summary>
    public const string PaperNumberIndex = "ux_questions_tenant_edition_paper_number";

    /// <summary>F-97 BR1: only a true/false question carries a true/false key.</summary>
    public const string TrueFalseKeyCheck = "ck_questions_true_false_key";

    public void Configure(EntityTypeBuilder<Question> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "questions",
            table =>
            {
                table.HasComment("A question of the bank, written by a curator. Its options are in question_options and, for a matching question, its left items in question_match_items.");
                table.HasCheckConstraint(
                    DifficultyCheck,
                    $"difficulty IS NULL OR difficulty BETWEEN {QuestionLimits.DifficultyMin} AND {QuestionLimits.DifficultyMax}");
                table.HasCheckConstraint(
                    OriginCheck,
                    "(exam_edition_id IS NULL AND paper_number IS NULL AND notice_subject_id IS NULL) OR "
                    + $"(exam_edition_id IS NOT NULL AND paper_number IS NOT NULL AND paper_number BETWEEN {QuestionLimits.PaperNumberMin} AND {QuestionLimits.PaperNumberMax})");
                table.HasCheckConstraint(TrueFalseKeyCheck, "true_false_key IS NULL OR type = 'TrueFalse'");
            });
        builder.HasKey(question => question.Id);

        builder.Property(question => question.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("How the question is answered: SingleChoice, TrueFalse, MultipleAnswer or Matching.");

        // F-101: a write that read another status than the stored one is a lost race (an annulment against an
        // unpublish, a save against an annulment) and answers 409 instead of last-writer-wins.
        builder.Property(question => question.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .IsConcurrencyToken()
            .HasComment("Where the question is in its life: Draft, InReview, Published or Annulled.");

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

        // F-102: the base text is shared, so the question holds a link to it, a foreign key inside the module.
        builder.Property(question => question.BaseTextId)
            .HasComment("The base text shown before the statement. A foreign key to base_texts; null when the question has none.");

        builder.HasOne<BaseText>()
            .WithMany()
            .HasForeignKey(question => question.BaseTextId)
            .HasConstraintName("fk_questions_base_text")
            .OnDelete(DeleteBehavior.Restrict);

        // The in-use check, the counts, the linked list and the fan-out of an edit filter by the base text alone.
        builder.HasIndex(question => question.BaseTextId)
            .HasDatabaseName("ix_questions_base_text");

        builder.Property(question => question.ExplanationHtml)
            .HasMaxLength(QuestionLimits.ExplanationMaxLength)
            .HasComment("Why the key is right, as sanitized HTML. Empty when there is none.");

        builder.Property(question => question.Difficulty)
            .HasComment("From 1 (easiest) to 5 (hardest). Empty when not set.");

        // F-96: the origin is three plain columns held by id, with no foreign key to the catalog (BR1).
        builder.Property(question => question.ExamEditionId)
            .HasComment("The past exam edition the question came from. Held by id; there is no foreign key to the catalog. Null means the question is authored.");

        builder.Property(question => question.PaperNumber)
            .HasComment("The number of the question in that edition's paper, from 1 to 500. Set exactly when exam_edition_id is.");

        builder.Property(question => question.NoticeSubjectId)
            .HasComment("The notice subject of that edition the question belongs to. Held by id; optional, and only with an edition.");

        // F-97: the key of a true/false question is one nullable boolean, not two option rows. Null means not set
        // yet, and is the only value a single-choice row can hold (the check above).
        builder.Property(question => question.TrueFalseKey)
            .HasComment("The key of a true/false question: true means right (certo), false wrong (errado), null not set yet. Always null for a single-choice question.");

        builder.HasMany(question => question.Options)
            .WithOne()
            .HasForeignKey(option => option.QuestionId)
            .HasConstraintName("fk_question_options_question")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(question => question.Options).UsePropertyAccessMode(PropertyAccessMode.Field);

        // F-106: the left items of a matching question, owned like the options.
        builder.HasMany(question => question.MatchItems)
            .WithOne()
            .HasForeignKey(item => item.QuestionId)
            .HasConstraintName("fk_question_match_items_question")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(question => question.MatchItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        // F-107 test phase: not mapped yet.
        builder.Ignore(question => question.Blanks);

        // The topic delete guard and the topic's "in use" flag filter by the topic alone.
        builder.HasIndex(question => question.TopicId)
            .HasDatabaseName("ix_questions_topic");

        // The list: a status filter, newest first.
        builder.HasIndex(question => new { question.Status, question.CreatedAt })
            .HasDatabaseName("ix_questions_status_created_at");

        // F-96 BR3: within one edition a live question's number is unique. The second clause of the filter is
        // needed because NULLS NOT DISTINCT would make every authored question (all three columns null) collide.
        builder.HasIndex(question => new { question.TenantId, question.ExamEditionId, question.PaperNumber })
            .HasDatabaseName(PaperNumberIndex)
            .IsUniquePerTenant()
            .HasFilter("is_deleted = false AND exam_edition_id IS NOT NULL");

        // The edition delete guard and the edition's question list filter by the edition alone; the unique
        // index cannot serve them because its first column is the tenant. The notice subject guard likewise.
        builder.HasIndex(question => question.ExamEditionId)
            .HasDatabaseName("ix_questions_exam_edition");
        builder.HasIndex(question => question.NoticeSubjectId)
            .HasDatabaseName("ix_questions_notice_subject");
    }
}
