using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-77: the other names of a subject. The normalized form is unique across every subject, NULLS NOT DISTINCT,
/// and — unlike the subject's own name — only over rows not deleted (BR8): an alias is a pointer, and a wrong
/// one must be fixable, so a removed alias frees its text. The foreign key is <c>Restrict</c>: a soft delete is
/// an UPDATE, so only a hard delete would reach it, and the handler removes the aliases with their subject.
/// </summary>
public sealed class SubjectAliasConfiguration : IEntityTypeConfiguration<SubjectAlias>
{
    public const string UniqueIndex = "ux_subject_aliases_tenant_normalized_name";

    /// <summary>The rows the unique index holds: the aliases still in use.</summary>
    public const string LiveFilter = "is_deleted = false";

    public void Configure(EntityTypeBuilder<SubjectAlias> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("subject_aliases", table => table.HasComment(
            "Another name a subject goes by, such as Portugues for Lingua Portuguesa."));
        builder.HasKey(alias => alias.Id);

        builder.Property(alias => alias.SubjectId)
            .IsRequired()
            .HasComment("The subject the other name points to.");

        builder.Property(alias => alias.Name)
            .HasMaxLength(CatalogLimits.SubjectNameMaxLength)
            .IsRequired()
            .HasComment("The other name as typed; content, so it is not translated.");

        builder.Property(alias => alias.NormalizedName)
            .HasMaxLength(CatalogLimits.SubjectNameMaxLength)
            .IsRequired()
            .HasComment("The other name without case or accents, which is what the unique index and the lookup compare.");

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(alias => alias.SubjectId)
            .HasConstraintName("fk_subject_aliases_subject")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(alias => new { alias.TenantId, alias.NormalizedName })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant()
            .HasFilter(LiveFilter);

        // Loading a subject's set, the list's alias search and the delete of a subject filter by the subject
        // alone, which the unique index cannot serve because its first column is the tenant.
        builder.HasIndex(alias => alias.SubjectId)
            .HasDatabaseName("ix_subject_aliases_subject");
    }
}
