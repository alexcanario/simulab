using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-166: one row per question of an attempt, written at the start; a blank question has a null answer, and clearing sets
/// it back to null. The answer is a JSON column with the question type as its discriminator, read whole and never queried
/// by content. The question is held by id (it belongs to the question bank).
/// </summary>
public sealed class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    /// <summary>One row per question of an attempt.</summary>
    public const string QuestionIndex = "ux_attempt_answers_tenant_attempt_question";

    public const string PaperNumberCheck = "ck_attempt_answers_paper_number_range";

    public const string VersionNumberCheck = "ck_attempt_answers_version_number_positive";

    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "attempt_answers",
            table =>
            {
                table.HasComment("One question of an attempt and the answer to it: the question, its paper number and the version the student saw are fixed at the start; the answer is null while the question is blank.");
                table.HasCheckConstraint(PaperNumberCheck, "paper_number IS NOT NULL AND paper_number BETWEEN 1 AND 500");
                table.HasCheckConstraint(VersionNumberCheck, "version_number IS NOT NULL AND version_number >= 1");
            });
        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.Id).ValueGeneratedNever();

        builder.Property(answer => answer.AttemptId)
            .IsRequired()
            .HasComment("The attempt the row belongs to.");

        builder.Property(answer => answer.QuestionId)
            .IsRequired()
            .HasComment("The question, a question bank question id. No foreign key: the question lives in another module.");

        builder.Property(answer => answer.PaperNumber)
            .IsRequired()
            .HasComment("The question's number in the paper, from 1 to 500; the rows of an attempt are shown in this order.");

        builder.Property(answer => answer.VersionNumber)
            .IsRequired()
            .HasComment("The number of the question version the student saw, fixed at the start; later edits never change it.");

        builder.Property(answer => answer.QuestionType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks, ShortAnswer, OpenAnswer or Essay.");

        var comparer = new ValueComparer<AnswerPayload?>(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => value);
        builder.Property(answer => answer.Answer)
            .HasConversion(
                new ValueConverter<AnswerPayload?, string?>(
                    value => value == null ? null : Serialize(value),
                    json => json == null ? null : JsonSerializer.Deserialize<AnswerPayload>(json, AppJson.Options)),
                comparer)
            // json and not jsonb: jsonb reorders the members of an object (a four-letter "text" sorts before "type"), and the
            // polymorphic read needs the type member first.
            .HasColumnType("json")
            .HasComment("The answer as JSON with a type member, first, that names its shape (the question type). Null while the question is blank.");

        // One row per question of an attempt; the tenant column makes it NULLS NOT DISTINCT.
        builder.HasIndex(answer => new { answer.TenantId, answer.AttemptId, answer.QuestionId })
            .HasDatabaseName(QuestionIndex)
            .IsUniquePerTenant();

        // The rows of an attempt are read by the attempt alone; the unique index leads with the tenant.
        builder.HasIndex(answer => answer.AttemptId)
            .HasDatabaseName("ix_attempt_answers_attempt");
    }

    private static string Serialize(AnswerPayload? value) => JsonSerializer.Serialize(value, AppJson.Options);
}
