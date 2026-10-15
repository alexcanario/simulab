using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-77: the other names of a topic. A name is unique inside its topic, NULLS NOT DISTINCT and over rows not
/// deleted (BR8); the rule across the topics of one subject (BR7) needs the topics' names too, so the handler
/// checks it by joining through <c>topics</c> — the table carries no subject of its own, and a move of the topic
/// to another subject needs no update here. The foreign key is <c>Restrict</c>: a soft delete is an UPDATE.
/// </summary>
public sealed class TopicAliasConfiguration : IEntityTypeConfiguration<TopicAlias>
{
    public const string UniqueIndex = "ux_topic_aliases_tenant_topic_normalized_name";

    /// <summary>The rows the unique indexes hold: the aliases still in use.</summary>
    public const string LiveFilter = "is_deleted = false";

    public void Configure(EntityTypeBuilder<TopicAlias> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("topic_aliases", table => table.HasComment(
            "Another name a topic goes by, such as Compreensao for Interpretacao de texto."));
        builder.HasKey(alias => alias.Id);

        builder.Property(alias => alias.TopicId)
            .IsRequired()
            .HasComment("The topic the other name points to.");

        builder.Property(alias => alias.Name)
            .HasMaxLength(CatalogLimits.TopicNameMaxLength)
            .IsRequired()
            .HasComment("The other name as typed; content, so it is not translated.");

        builder.Property(alias => alias.NormalizedName)
            .HasMaxLength(CatalogLimits.TopicNameMaxLength)
            .IsRequired()
            .HasComment("The other name without case or accents, which is what the unique index and the lookup compare.");

        builder.HasOne<Topic>()
            .WithMany()
            .HasForeignKey(alias => alias.TopicId)
            .HasConstraintName("fk_topic_aliases_topic")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(alias => new { alias.TenantId, alias.TopicId, alias.NormalizedName })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant()
            .HasFilter(LiveFilter);

        // Loading a topic's set, the topics list and the delete of a topic filter by the topic alone, which the
        // unique index cannot serve because its first column is the tenant.
        builder.HasIndex(alias => alias.TopicId)
            .HasDatabaseName("ix_topic_aliases_topic");

        // The lookup by name and the per-subject clash check compare the normalized form without a topic.
        builder.HasIndex(alias => alias.NormalizedName)
            .HasDatabaseName("ix_topic_aliases_normalized_name")
            .HasFilter(LiveFilter);
    }
}
