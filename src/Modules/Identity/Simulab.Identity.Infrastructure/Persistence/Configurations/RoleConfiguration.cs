using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("roles");
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name).HasMaxLength(256);
        builder.Property(role => role.NormalizedName).HasMaxLength(256);
        builder.Property(role => role.ConcurrencyStamp).IsConcurrencyToken();

        // F-9, BR3: not filtered by IsDeleted on purpose - a deleted role's name stays taken.
        builder.HasIndex(role => role.NormalizedName).HasDatabaseName("ux_roles_normalized_name").IsUnique();
    }
}
