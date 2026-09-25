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

        builder.ToTable(AiDbContext.TableName, AiDbContext.SchemaName, table => table.HasComment(
            "One call that reached the model, answered or failed. A call the gateway refused before it left the app is not here: nothing was spent."));
        builder.HasKey(call => call.Id);

        builder.Property(call => call.UserId).IsRequired()
            .HasComment("Who the call is charged to. Every call belongs to a signed-in user.");
        builder.Property(call => call.Purpose).IsRequired().HasMaxLength(AiPurposes.MaxLength)
            .HasComment("What the call was for, such as diagnostics. Cost can be read per purpose, and a purpose can be sent to another model from configuration.");
        builder.Property(call => call.Model).IsRequired().HasMaxLength(ModelMaxLength)
            .HasComment("The model that was called, as the provider names it.");
        builder.Property(call => call.InputTokens).IsRequired()
            .HasComment("Tokens the request spent, as the answer reports them. Zero on a failed call, where none are known.");
        builder.Property(call => call.OutputTokens).IsRequired()
            .HasComment("Tokens the answer spent, as the answer reports them. Zero on a failed call, where none are known.");
        builder.Property(call => call.InputPricePerMillion).IsRequired().HasColumnType(MoneyColumnType)
            .HasComment("US dollars per million input tokens, as configured when the call was made. Kept on the row so a later price change never rewrites the past.");
        builder.Property(call => call.OutputPricePerMillion).IsRequired().HasColumnType(MoneyColumnType)
            .HasComment("US dollars per million output tokens, as configured when the call was made.");
        builder.Property(call => call.CostUsd).IsRequired().HasColumnType(MoneyColumnType)
            .HasComment("What the call cost in US dollars: the tokens multiplied by the two prices on this row.");
        builder.Property(call => call.DurationMs).IsRequired()
            .HasComment("How long the call took, in milliseconds, measured around the request.");
        builder.Property(call => call.Succeeded).IsRequired()
            .HasComment("False when the model answered with an error or could not be reached. Only successful calls count against the monthly quota.");
        builder.Property(call => call.ErrorCode).HasMaxLength(ErrorCodeMaxLength)
            .HasComment("Why the call failed, as a stable code such as ai.call_failed. Null when it succeeded.");
        builder.Property(call => call.StartedAt).IsRequired()
            .HasComment("When the call left the app, in UTC. The monthly quota counts from the first day of the calendar month.");

        // BR3: the gateway counts one user's successful calls in the current month before every call.
        builder.HasIndex(call => new { call.UserId, call.StartedAt })
            .HasDatabaseName("ix_ai_calls_user_started_at");
    }
}
