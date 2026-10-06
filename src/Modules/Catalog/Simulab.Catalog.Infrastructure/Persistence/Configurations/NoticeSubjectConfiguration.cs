using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-74: the notice subjects table. A label is unique inside its group and edition, by their normalized
/// forms, NULLS NOT DISTINCT, and — unlike the other catalog indexes — only over rows not deleted (BR5):
/// an admin who deletes a row by mistake must be able to add it again. The normalized group is empty when
/// there is none, so "no group" is one group. The foreign key is <c>Restrict</c>: a soft delete is an UPDATE,
/// so only a hard delete would reach it.
/// </summary>
public sealed class NoticeSubjectConfiguration : IEntityTypeConfiguration<NoticeSubject>
{
    public const string UniqueIndex = "ux_notice_subjects_tenant_edition_group_label";

    /// <summary>The rows the unique index holds: the ones still in the notice.</summary>
    public const string LiveFilter = "is_deleted = false";

    public void Configure(EntityTypeBuilder<NoticeSubject> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notice_subjects", table => table.HasComment(
            "A subject as one edition's notice names and groups it, with the number of questions the notice states."));
        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.ExamEditionId)
            .IsRequired()
            .HasComment("The edition whose notice names the subject. Fixed when the row is created.");

        // "group" is a reserved word, so the column keeps the name the item gave it.
        builder.Property(subject => subject.Group)
            .HasColumnName("group_label")
            .HasMaxLength(CatalogLimits.NoticeSubjectGroupMaxLength)
            .HasComment("How the notice groups the subject, as typed. Empty when the notice does not group.");

        builder.Property(subject => subject.NormalizedGroup)
            .HasMaxLength(CatalogLimits.NoticeSubjectGroupMaxLength)
            .IsRequired()
            .HasComment("The group without case or accents, empty when there is none. The unique index compares it.");

        builder.Property(subject => subject.Label)
            .HasMaxLength(CatalogLimits.NoticeSubjectLabelMaxLength)
            .IsRequired()
            .HasComment("The subject as the notice names it.");

        builder.Property(subject => subject.NormalizedLabel)
            .HasMaxLength(CatalogLimits.NoticeSubjectLabelMaxLength)
            .IsRequired()
            .HasComment("The label without case or accents. The unique index compares it.");

        builder.Property(subject => subject.QuestionCount)
            .HasComment("How many questions the notice states for the subject, 1 to 500. Empty when it does not say.");

        builder.Property(subject => subject.DisplayOrder)
            .IsRequired()
            .HasComment("Where the row sits inside its edition. Rows of one group are consecutive; every write renumbers from 1.");

        builder.HasOne<ExamEdition>()
            .WithMany()
            .HasForeignKey(subject => subject.ExamEditionId)
            .HasConstraintName("fk_notice_subjects_exam_edition")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subject => new
            {
                subject.TenantId,
                subject.ExamEditionId,
                subject.NormalizedGroup,
                subject.NormalizedLabel
            })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant()
            .HasFilter(LiveFilter);

        // The edition's list, the sibling load of every write and the cascade of an edition delete filter by
        // the edition and read the order, which the unique index cannot serve because its first column is the tenant.
        builder.HasIndex(subject => new { subject.ExamEditionId, subject.DisplayOrder })
            .HasDatabaseName("ix_notice_subjects_edition_order");
    }
}
