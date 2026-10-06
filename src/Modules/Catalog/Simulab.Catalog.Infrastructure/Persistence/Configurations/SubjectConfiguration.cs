using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-79: the subjects table. The unique index is over the normalized name, includes <c>TenantId</c> and is
/// NULLS NOT DISTINCT (catalog rows are global). Deleted rows are in the index: a deleted subject keeps its
/// name taken, as the rest of the catalog does (F-33 BR9, BR11).
/// </summary>
public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public const string UniqueIndex = "ux_subjects_tenant_normalized_name";

    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("subjects", table => table.HasComment(
            "A subject of the canonical taxonomy, such as Portuguese or Constitutional Law, that topics hang under."));
        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.Name)
            .HasMaxLength(CatalogLimits.SubjectNameMaxLength)
            .IsRequired()
            .HasComment("The name as typed; content, so it is not translated.");

        builder.Property(subject => subject.NormalizedName)
            .HasMaxLength(CatalogLimits.SubjectNameMaxLength)
            .IsRequired()
            .HasComment("The name without case or accents, which is what the unique index compares.");

        builder.Property(subject => subject.AreaId)
            .HasComment("The area the subject belongs to; empty when it has none.");

        builder.HasOne<Area>()
            .WithMany()
            .HasForeignKey(subject => subject.AreaId)
            .HasConstraintName("fk_subjects_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subject => new { subject.TenantId, subject.NormalizedName })
            .HasDatabaseName(UniqueIndex)
            .IsUniquePerTenant();

        // The area filter reads by area alone, which the unique index cannot serve: its first column is the tenant.
        builder.HasIndex(subject => subject.AreaId)
            .HasDatabaseName("ix_subjects_area");
    }
}
