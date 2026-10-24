using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Persistence;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-98: the immutable snapshots of a published question. The options are a JSON column of the row, read whole
/// and never queried by option (the data dictionary names their shape: position, text and mark). The key to the
/// question stays inside the module and restricts a delete: a version is never removed, and a question is only
/// ever soft-deleted (BR6).
/// </summary>
public sealed class QuestionVersionConfiguration : IEntityTypeConfiguration<QuestionVersion>
{
    /// <summary>F-98 BR7, BR10: the unique number of a version inside its question.</summary>
    public const string NumberIndex = "ux_question_versions_tenant_question_number";

    public const string NumberCheck = "ck_question_versions_number_positive";

    /// <summary>F-98 BR5: a version whose key changed always carries the reason.</summary>
    public const string ReasonCheck = "ck_question_versions_key_change_reason";

    /// <summary>F-101 BR2: an annulled version always carries the reason.</summary>
    public const string AnnulledReasonCheck = "ck_question_versions_annulled_reason";

    /// <summary>F-101 BR2: an annulment is never also an answer key change.</summary>
    public const string AnnulledKeyChangedCheck = "ck_question_versions_annulled_key_changed";

    public void Configure(EntityTypeBuilder<QuestionVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_versions",
            table =>
            {
                table.HasComment("An immutable snapshot of a published question: what students were shown and graded on, numbered from 1 for the question. Never edited or deleted.");
                table.HasCheckConstraint(NumberCheck, "number >= 1");
                table.HasCheckConstraint(ReasonCheck, "answer_key_changed = false OR reason IS NOT NULL");
                table.HasCheckConstraint(AnnulledReasonCheck, "annulled = false OR reason IS NOT NULL");
                table.HasCheckConstraint(AnnulledKeyChangedCheck, "annulled = false OR answer_key_changed = false");
            });
        builder.HasKey(version => version.Id);

        // The entity makes its own id, as the question and its options do.
        builder.Property(version => version.Id).ValueGeneratedNever();

        builder.Property(version => version.QuestionId)
            .IsRequired()
            .HasComment("The question the version belongs to.");

        builder.Property(version => version.Number)
            .IsRequired()
            .HasComment("1, 2, ...; unique for the question. The latest version is the highest number.");

        builder.Property(version => version.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks or ShortAnswer.");

        builder.Property(version => version.ContentLanguage)
            .HasMaxLength(QuestionLimits.ContentLanguageMaxLength)
            .IsRequired()
            .HasComment("The language the content was written in, such as pt-BR.");

        builder.Property(version => version.StatementHtml)
            .HasMaxLength(QuestionLimits.StatementMaxLength)
            .IsRequired()
            .HasComment("The statement as sanitized HTML, as it was when the version was recorded.");

        builder.Property(version => version.BaseTextHtml)
            .HasMaxLength(QuestionLimits.BaseTextMaxLength)
            .HasComment("The base text as sanitized HTML. Empty when there was none.");

        builder.Property(version => version.ExplanationHtml)
            .HasMaxLength(QuestionLimits.ExplanationMaxLength)
            .HasComment("The explanation as sanitized HTML. Empty when there was none.");

        builder.Property(version => version.TrueFalseKey)
            .HasComment("The key of a true/false question: true means right (certo), false wrong (errado). Null for a single-choice question.");

        builder.Property(version => version.AnswerKeyChanged)
            .IsRequired()
            .HasComment("True when the answer key differs from the previous version's. Never true for version 1.");

        builder.Property(version => version.Reason)
            .HasMaxLength(QuestionLimits.ChangeReasonMaxLength)
            .HasComment("Why the curator changed the answer key, annulled or reinstated the question, or a note on the change. Always set when answer_key_changed or annulled is true.");

        builder.Property(version => version.Annulled)
            .IsRequired()
            .HasComment("True when the question was annulled when this version was recorded. A reinstatement is the first version after an annulled one with this false.");

        // BR1: the options are a copy read whole: a JSON column with no property behind it, so no comment can be
        // attached to it (the architecture test names this exemption).
        builder.OwnsMany(version => version.Options, options => options.ToJson("options"));
        builder.Navigation(version => version.Options).UsePropertyAccessMode(PropertyAccessMode.Field);

        // F-106 BR8: the left items of a matching question, a second JSON column read whole; null on the versions
        // recorded before it, which the entity reads as none (the architecture test names this exemption too).
        builder.OwnsMany(version => version.MatchItems, items => items.ToJson("match_items"));
        builder.Navigation(version => version.MatchItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        // F-107 BR10: the blanks of a fill-in-the-blanks question in statement order, a third JSON column read whole; null
        // on the versions recorded before it, which the entity reads as none (the architecture test names this exemption).
        builder.OwnsMany(version => version.Blanks, blanks => blanks.ToJson("blanks"));
        builder.Navigation(version => version.Blanks).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(version => version.QuestionId)
            .HasConstraintName("fk_question_versions_question")
            .OnDelete(DeleteBehavior.Restrict);

        // BR7, BR10: the number is unique per question; the tenant column makes it NULLS NOT DISTINCT.
        builder.HasIndex(version => new { version.TenantId, version.QuestionId, version.Number })
            .HasDatabaseName(NumberIndex)
            .IsUniquePerTenant();

        // The latest version, the list and the current number filter by the question alone; the unique index
        // cannot serve them because its first column is the tenant.
        builder.HasIndex(version => version.QuestionId)
            .HasDatabaseName("ix_question_versions_question");
    }
}
