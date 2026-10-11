using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-95: the options table. There is no unique index over (question, position): the question writes its options
/// as one list, and a move swaps two live positions, which a non-deferrable unique index refuses depending on
/// the order EF writes the statements. The question keeps the positions contiguous.
/// </summary>
public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("question_options", table => table.HasComment("One option of a question, in position order."));
        builder.HasKey(option => option.Id);

        // The entity makes its own id. An option added to a tracked question is found by change detection, and
        // for a store-generated key that is already set EF would take it for an existing row and UPDATE it.
        builder.Property(option => option.Id).ValueGeneratedNever();

        builder.Property(option => option.QuestionId)
            .IsRequired()
            .HasComment("The question the option belongs to.");

        builder.Property(option => option.Position)
            .IsRequired()
            .HasComment("Where the option sits, from 0. The letter A to E is the position and is never stored.");

        builder.Property(option => option.TextHtml)
            .HasMaxLength(QuestionLimits.OptionMaxLength)
            .IsRequired()
            .HasComment("The option text as sanitized HTML. Empty only on a draft.");

        builder.Property(option => option.IsCorrect)
            .IsRequired()
            .HasComment("True for the answer key. A published question has exactly one.");

        builder.HasIndex(option => option.QuestionId)
            .HasDatabaseName("ix_question_options_question");
    }
}
