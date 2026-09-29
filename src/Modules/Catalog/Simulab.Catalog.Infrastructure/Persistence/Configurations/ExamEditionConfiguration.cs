using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-35: the exam editions table. The unique index is over the exam, the notice year, the normalized position
/// and the board (BR10), so a paper annulled and applied again by another board in the same year is still
/// allowed. It includes <c>TenantId</c> and is NULLS NOT DISTINCT: catalog rows are global, and without it
/// two null tenants would count as different keys (ADR-0001, decision 8). The normalized position is empty
/// when there is none, so "no position" collides with "no position". Deleted rows are in the index too.
/// </summary>
public sealed class ExamEditionConfiguration : IEntityTypeConfiguration<ExamEdition>
{
    public const string UniqueIndex = "ux_exam_editions_tenant_exam_year_position_organizer";

    public void Configure(EntityTypeBuilder<ExamEdition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("exam_editions", table => table.HasComment(
            "One paper actually applied for an exam: its notice year, the job it selects for, the board that applied it and whether students can see it."));
        builder.HasKey(edition => edition.Id);

        builder.Property(edition => edition.ExamId)
            .IsRequired()
            .HasComment("The exam this paper belongs to. Fixed when the edition is created.");

        builder.Property(edition => edition.OrganizerId)
            .IsRequired()
            .HasComment("The exam board that applied the paper, as the notice names it.");

        builder.Property(edition => edition.NoticeYear)
            .IsRequired()
            .HasComment("The year of the notice, from 1990 to next year.");

        builder.Property(edition => edition.Position)
            .HasMaxLength(CatalogLimits.ExamEditionPositionMaxLength)
            .HasComment("The job the paper selects for. Empty for ENEM, entrance exams and certifications.");

        builder.Property(edition => edition.NormalizedPosition)
            .HasMaxLength(CatalogLimits.ExamEditionPositionMaxLength)
            .IsRequired()
            .HasComment("The position without case or accents, empty when there is none. The unique index compares it.");

        builder.Property(edition => edition.NoticeReference)
            .HasMaxLength(CatalogLimits.ExamEditionNoticeReferenceMaxLength)
            .HasComment("How the notice names itself, such as Edital nº 01/2026. Editions cut from one notice share it.");

        builder.Property(edition => edition.NoticeUrl)
            .HasMaxLength(CatalogLimits.ExamEditionNoticeUrlMaxLength)
            .HasComment("The official address of the notice, an absolute http or https URL.");

        builder.Property(edition => edition.AppliedOn)
            .HasComment("The day the paper was applied. Optional, also to publish.");

        builder.Property(edition => edition.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("Who can see the edition: Draft or Published.");

        // BR12: the exam and the board cannot be deleted while an edition points at them, and the handlers
        // answer that with a 409. Restrict is the database saying the same thing, in case anything ever
        // bypasses them (a soft delete is an UPDATE, so only a hard delete would reach it).
        builder.HasOne<Exam>()
            .WithMany()
            .HasForeignKey(edition => edition.ExamId)
            .HasConstraintName("fk_exam_editions_exam")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organizer>()
            .WithMany()
            .HasForeignKey(edition => edition.OrganizerId)
            .HasConstraintName("fk_exam_editions_organizer")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(edition => new
            {
                edition.TenantId,
                edition.ExamId,
                edition.NoticeYear,
                edition.NormalizedPosition,
                edition.OrganizerId
            })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant();

        // BR13: "board X, years A to B" for the simulator (epic 695).
        builder.HasIndex(edition => new { edition.OrganizerId, edition.NoticeYear })
            .HasDatabaseName("ix_exam_editions_organizer_year");

        // The exam's list, the delete guard and the duplicate check filter by the exam alone, which the
        // unique index cannot serve because its first column is the tenant.
        builder.HasIndex(edition => edition.ExamId)
            .HasDatabaseName("ix_exam_editions_exam");
    }
}
