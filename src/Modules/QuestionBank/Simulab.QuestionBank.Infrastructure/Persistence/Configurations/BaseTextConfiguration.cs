using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-102: the base texts table. A text is shared by the questions that point at it through
/// <c>questions.base_text_id</c>, a foreign key inside the module that restricts a hard delete; the app only
/// ever soft-deletes, and a text that a live question links is refused (BR6). The index serves the picker, which
/// always filters by language and sorts by title.
/// </summary>
public sealed class BaseTextConfiguration : IEntityTypeConfiguration<BaseText>
{
    /// <summary>BR1: a title is never blank.</summary>
    public const string TitleCheck = "ck_base_texts_title_not_blank";

    /// <summary>BR1: a text is never empty.</summary>
    public const string TextCheck = "ck_base_texts_text_not_blank";

    /// <summary>BR11: the picker and the list filter by language and sort by title.</summary>
    public const string LanguageTitleIndex = "ix_base_texts_content_language_normalized_title";

    public void Configure(EntityTypeBuilder<BaseText> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "base_texts",
            table =>
            {
                table.HasComment("A passage, table or case that several questions refer to, written once by a curator. A question points at it through questions.base_text_id.");
                table.HasCheckConstraint(TitleCheck, "char_length(btrim(title)) > 0");
                table.HasCheckConstraint(TextCheck, "text_html <> ''");
            });
        builder.HasKey(baseText => baseText.Id);

        // The entity makes its own id, as the question and its versions do.
        builder.Property(baseText => baseText.Id).ValueGeneratedNever();

        builder.Property(baseText => baseText.Title)
            .HasMaxLength(QuestionLimits.BaseTextTitleMaxLength)
            .IsRequired()
            .HasComment("What the curator calls the text, trimmed. The list and the picker show it.");

        builder.Property(baseText => baseText.NormalizedTitle)
            .HasMaxLength(QuestionLimits.BaseTextTitleMaxLength)
            .IsRequired()
            .HasComment("The title without case or accents. The list search reads it; derived on every write.");

        builder.Property(baseText => baseText.ContentLanguage)
            .HasMaxLength(QuestionLimits.ContentLanguageMaxLength)
            .IsRequired()
            .HasComment("The language the text is written in, as a code such as pt-BR. It is never translated, and the questions that link it are in the same language.");

        builder.Property(baseText => baseText.TextHtml)
            .HasMaxLength(QuestionLimits.BaseTextMaxLength)
            .IsRequired()
            .HasComment("The text, as sanitized HTML.");

        builder.HasIndex(baseText => new { baseText.ContentLanguage, baseText.NormalizedTitle })
            .HasDatabaseName(LanguageTitleIndex);
    }
}
