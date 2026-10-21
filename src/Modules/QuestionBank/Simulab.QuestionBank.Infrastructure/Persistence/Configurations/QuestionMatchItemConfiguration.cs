using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-106: the left items of the matching questions. As for the options, there is no unique index over (question,
/// position): a move swaps two live positions, which a non-deferrable unique index refuses depending on the order
/// EF writes the statements. The question keeps the positions contiguous and the matches one to one.
/// </summary>
public sealed class QuestionMatchItemConfiguration : IEntityTypeConfiguration<QuestionMatchItem>
{
    public void Configure(EntityTypeBuilder<QuestionMatchItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "question_match_items",
            table => table.HasComment("One numbered left item of a matching question, in position order. The right column is the question's options."));
        builder.HasKey(item => item.Id);

        // The entity makes its own id, as an option does.
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.QuestionId)
            .IsRequired()
            .HasComment("The matching question the item belongs to.");

        builder.Property(item => item.Position)
            .IsRequired()
            .HasComment("Where the item sits, from 0. The number 1 to 8 is the position and is never stored.");

        builder.Property(item => item.TextHtml)
            .HasMaxLength(QuestionLimits.OptionMaxLength)
            .IsRequired()
            .HasComment("The item text as sanitized HTML. Empty only on a draft.");

        builder.Property(item => item.MatchPosition)
            .HasComment("The position of the option (right item) the item matches; the only copy of the key. Null only on a draft.");

        builder.HasIndex(item => item.QuestionId)
            .HasDatabaseName("ix_question_match_items_question");
    }
}
