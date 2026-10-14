using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-75: the mapping of a notice subject to the canonical taxonomy. One row is either a whole subject or one
/// topic (a check constraint). The unique index holds each target once per notice subject, NULLS NOT DISTINCT,
/// and only over live rows, so a dropped entry can be added again. Foreign keys to the taxonomy are
/// <c>Restrict</c>: a soft delete is an UPDATE, so only a hard delete would reach them, and the handlers refuse
/// the delete of a mapped item first (BR9). The foreign key to the notice subject cascades a hard delete only.
/// </summary>
public sealed class NoticeSubjectMappingConfiguration : IEntityTypeConfiguration<NoticeSubjectMapping>
{
    public const string UniqueIndex = "ux_notice_subject_mappings_tenant_notice_subject_target";

    public const string OneTargetCheck = "ck_notice_subject_mappings_one_target";

    public void Configure(EntityTypeBuilder<NoticeSubjectMapping> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "notice_subject_mappings",
            table =>
            {
                table.HasComment("One canonical subject or topic that a notice subject covers.");
                table.HasCheckConstraint(OneTargetCheck, "num_nonnulls(subject_id, topic_id) = 1");
            });
        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.NoticeSubjectId)
            .IsRequired()
            .HasComment("The notice subject whose mapping this entry belongs to.");

        builder.Property(mapping => mapping.SubjectId)
            .HasComment("The canonical subject covered whole. Empty when the entry is a topic.");

        builder.Property(mapping => mapping.TopicId)
            .HasComment("The canonical topic covered. Empty when the entry is a whole subject. Follows the topic when it moves to another subject.");

        builder.HasOne<NoticeSubject>()
            .WithMany()
            .HasForeignKey(mapping => mapping.NoticeSubjectId)
            .HasConstraintName("fk_notice_subject_mappings_notice_subject")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(mapping => mapping.SubjectId)
            .HasConstraintName("fk_notice_subject_mappings_subject")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Topic>()
            .WithMany()
            .HasForeignKey(mapping => mapping.TopicId)
            .HasConstraintName("fk_notice_subject_mappings_topic")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mapping => new { mapping.TenantId, mapping.NoticeSubjectId, mapping.SubjectId, mapping.TopicId })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant()
            .HasFilter(NoticeSubjectConfiguration.LiveFilter);

        // The row's mapping load, and the list of an edition, filter by the notice subject alone, which the
        // unique index cannot serve because its first column is the tenant.
        builder.HasIndex(mapping => mapping.NoticeSubjectId)
            .HasDatabaseName("ix_notice_subject_mappings_notice_subject");

        // The delete guard of a subject or a topic (BR9) asks "is anything mapped to it".
        builder.HasIndex(mapping => mapping.SubjectId)
            .HasDatabaseName("ix_notice_subject_mappings_subject");
        builder.HasIndex(mapping => mapping.TopicId)
            .HasDatabaseName("ix_notice_subject_mappings_topic");
    }
}
