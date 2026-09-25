using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-33: the organizers table. The two unique indexes are over the normalized columns (BR9), so a
/// name that differs only in case or accents is refused by the database and not only by the handler.
/// Both include <c>TenantId</c> and are NULLS NOT DISTINCT: catalog rows are global, and without it
/// two null tenants would count as different keys (ADR-0001, decision 8).
/// </summary>
public sealed class OrganizerConfiguration : IEntityTypeConfiguration<Organizer>
{
    public void Configure(EntityTypeBuilder<Organizer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("organizers", table => table.HasComment(
            "Who runs an assessment: an exam board, a certifying body or a university. An exam belongs to one of them."));
        builder.HasKey(organizer => organizer.Id);

        builder.Property(organizer => organizer.Name)
            .HasMaxLength(CatalogLimits.OrganizerNameMaxLength)
            .IsRequired()
            .HasComment("Full name, as the organizer writes it.");

        builder.Property(organizer => organizer.Acronym)
            .HasMaxLength(CatalogLimits.OrganizerAcronymMaxLength)
            .IsRequired()
            .HasComment("Short name people search by, such as CEBRASPE or FGV.");

        builder.Property(organizer => organizer.Kind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("What kind of organizer it is: ExamBoard, CertifyingBody or University.");

        builder.Property(organizer => organizer.Description)
            .HasMaxLength(CatalogLimits.OrganizerDescriptionMaxLength)
            .HasComment("Free notes about the organizer, shown to whoever curates the catalog.");

        builder.Property(organizer => organizer.Website)
            .HasMaxLength(CatalogLimits.OrganizerWebsiteMaxLength)
            .HasComment("The organizer's own address, where its notices are published.");

        builder.Property(organizer => organizer.NormalizedName)
            .HasMaxLength(CatalogLimits.OrganizerNameMaxLength)
            .IsRequired()
            .HasComment("The name without case or accents, which is what the unique index compares.");

        builder.Property(organizer => organizer.NormalizedAcronym)
            .HasMaxLength(CatalogLimits.OrganizerAcronymMaxLength)
            .IsRequired()
            .HasComment("The acronym without case or accents, which is what the unique index compares.");

        // BR9: a deleted row keeps its name taken, so the indexes cover deleted rows too.
        builder.HasIndex(organizer => new { organizer.TenantId, organizer.NormalizedName })
            .HasDatabaseName("ux_organizers_tenant_normalized_name")
            .IsUniquePerTenant();

        builder.HasIndex(organizer => new { organizer.TenantId, organizer.NormalizedAcronym })
            .HasDatabaseName("ux_organizers_tenant_normalized_acronym")
            .IsUniquePerTenant();
    }
}
