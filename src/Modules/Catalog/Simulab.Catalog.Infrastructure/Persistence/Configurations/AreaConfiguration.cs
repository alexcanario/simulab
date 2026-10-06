using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-79 BR1: the fixed list of areas a subject may belong to. The migration seeds it and no screen edits it.
/// The code is unique per tenant, NULLS NOT DISTINCT like every catalog index over a nullable tenant.
/// </summary>
public sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("areas", table => table.HasComment(
            "The fixed list of areas a subject may belong to, seeded by a migration; its names live in the resource files."));
        builder.HasKey(area => area.Id);

        builder.Property(area => area.Code)
            .HasMaxLength(CatalogLimits.AreaCodeMaxLength)
            .IsRequired()
            .HasComment("Stable code the UI names the area by, such as Law; the resource key is Area.<Code>.");

        builder.Property(area => area.DisplayOrder)
            .IsRequired()
            .HasComment("Where the area sits in the list, starting at 1.");

        builder.HasIndex(area => new { area.TenantId, area.Code })
            .HasDatabaseName("ux_areas_tenant_code")
            .IsUniquePerTenant();
    }
}
