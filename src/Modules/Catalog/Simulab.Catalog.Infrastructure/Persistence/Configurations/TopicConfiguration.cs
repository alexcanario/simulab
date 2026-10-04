using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-79: the topics table. A name is unique inside its subject, by its normalized form, NULLS NOT DISTINCT
/// and with deleted rows included (BR7). The foreign key is <c>Restrict</c>: the handler answers 409
/// <c>subject.has_topics</c>, and this is the database saying the same in case anything bypasses it (a soft
/// delete is an UPDATE, so only a hard delete would reach it).
/// </summary>
public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public const string UniqueIndex = "ux_topics_tenant_subject_normalized_name";

    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("topics", table => table.HasComment(
            "A topic under a subject, such as Crase under Portuguese."));
        builder.HasKey(topic => topic.Id);

        builder.Property(topic => topic.SubjectId)
            .IsRequired()
            .HasComment("The subject the topic sits under; an edit may move it to another.");

        builder.Property(topic => topic.Name)
            .HasMaxLength(CatalogLimits.TopicNameMaxLength)
            .IsRequired()
            .HasComment("The name as typed; content, so it is not translated.");

        builder.Property(topic => topic.NormalizedName)
            .HasMaxLength(CatalogLimits.TopicNameMaxLength)
            .IsRequired()
            .HasComment("The name without case or accents, which is what the unique index compares.");

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(topic => topic.SubjectId)
            .HasConstraintName("fk_topics_subject")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(topic => new { topic.TenantId, topic.SubjectId, topic.NormalizedName })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant();

        // The subject's topic list, its topic count and the delete guard filter by the subject alone, which the
        // unique index cannot serve because its first column is the tenant.
        builder.HasIndex(topic => topic.SubjectId)
            .HasDatabaseName("ix_topics_subject");
    }
}
