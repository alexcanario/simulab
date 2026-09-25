using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-34 v2: the issuing authorities table. The two unique indexes are over the normalized columns, so a
/// name that differs only in case or accents is refused by the database and not only by the handler. Both
/// include <c>TenantId</c> and are NULLS NOT DISTINCT: catalog rows are global, and without it two null
/// tenants would count as different keys (ADR-0001, decision 8). Deleted rows are in the index: a deleted
/// body keeps its name taken, exactly as the organizer does (F-33 BR9, BR11).
/// </summary>
public sealed class IssuingAuthorityConfiguration : IEntityTypeConfiguration<IssuingAuthority>
{
    public void Configure(EntityTypeBuilder<IssuingAuthority> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("issuing_authorities", table => table.HasComment(
            "The body that publishes a notice and contracts an organizer to run the exam: a ministry, a court, a city hall."));
        builder.HasKey(authority => authority.Id);

        builder.Property(authority => authority.Name)
            .HasMaxLength(CatalogLimits.IssuingAuthorityNameMaxLength)
            .IsRequired()
            .HasComment("Full name, as the notice writes it.");

        builder.Property(authority => authority.Acronym)
            .HasMaxLength(CatalogLimits.IssuingAuthorityAcronymMaxLength)
            .IsRequired()
            .HasComment("Short name people search by, such as TRF1 or INSS.");

        builder.Property(authority => authority.Description)
            .HasMaxLength(CatalogLimits.IssuingAuthorityDescriptionMaxLength)
            .HasComment("Free notes about the body, shown to whoever curates the catalog.");

        builder.Property(authority => authority.Website)
            .HasMaxLength(CatalogLimits.IssuingAuthorityWebsiteMaxLength)
            .HasComment("The body's own address, where its notices are published.");

        builder.Property(authority => authority.NormalizedName)
            .HasMaxLength(CatalogLimits.IssuingAuthorityNameMaxLength)
            .IsRequired()
            .HasComment("The name without case or accents, which is what the unique index compares.");

        builder.Property(authority => authority.NormalizedAcronym)
            .HasMaxLength(CatalogLimits.IssuingAuthorityAcronymMaxLength)
            .IsRequired()
            .HasComment("The acronym without case or accents, which is what the unique index compares.");

        builder.HasIndex(authority => new { authority.TenantId, authority.NormalizedName })
            .HasDatabaseName("ux_issuing_authorities_tenant_normalized_name")
            .IsUniquePerTenant();

        builder.HasIndex(authority => new { authority.TenantId, authority.NormalizedAcronym })
            .HasDatabaseName("ux_issuing_authorities_tenant_normalized_acronym")
            .IsUniquePerTenant();
    }
}
