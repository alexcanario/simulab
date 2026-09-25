using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Ai.Contracts;

namespace Simulab.Ai.Persistence.Configurations;

/// <summary>The one table of the gateway (F-41, BR5).</summary>
public sealed class AiCallConfiguration : IEntityTypeConfiguration<AiCall>
{
    /// <summary>Longest model id the column keeps.</summary>
    public const int ModelMaxLength = 100;

    /// <summary>Longest error code the column keeps; the codes are short and fixed.</summary>
    public const int ErrorCodeMaxLength = 100;

    /// <summary>Money is exact: dollars with six decimal places, so a call of a few cents is not rounded away.</summary>
    private const string MoneyColumnType = "numeric(18,6)";

    public void Configure(EntityTypeBuilder<AiCall> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(AiDbContext.TableName, AiDbContext.SchemaName);
        builder.HasKey(call => call.Id);

        builder.Property(call => call.UserId).IsRequired();
        builder.Property(call => call.Purpose).IsRequired().HasMaxLength(AiPurposes.MaxLength);
        builder.Property(call => call.Model).IsRequired().HasMaxLength(ModelMaxLength);
        builder.Property(call => call.InputTokens).IsRequired();
        builder.Property(call => call.OutputTokens).IsRequired();
        builder.Property(call => call.InputPricePerMillion).IsRequired().HasColumnType(MoneyColumnType);
        builder.Property(call => call.OutputPricePerMillion).IsRequired().HasColumnType(MoneyColumnType);
        builder.Property(call => call.CostUsd).IsRequired().HasColumnType(MoneyColumnType);
        builder.Property(call => call.DurationMs).IsRequired();
        builder.Property(call => call.Succeeded).IsRequired();
        builder.Property(call => call.ErrorCode).HasMaxLength(ErrorCodeMaxLength);
        builder.Property(call => call.StartedAt).IsRequired();

        // BR3: the gateway counts one user's successful calls in the current month before every call.
        builder.HasIndex(call => new { call.UserId, call.StartedAt })
            .HasDatabaseName("ix_ai_calls_user_started_at");
    }
}
