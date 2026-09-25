using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("permissions", table => table.HasComment(
            "Everything a role can be allowed to do. Each module declares its own names and Identity seeds the union of them."));
        builder.HasKey(permission => permission.Name);

        builder.Property(permission => permission.Name).HasMaxLength(100)
            .HasComment("The permission name and the key of the row, such as catalog.manage. The part before the first dot is the module that declares it.");
        builder.Property(permission => permission.Description).HasMaxLength(500)
            .HasComment("What the permission allows, shown to an admin choosing permissions for a role.");
    }
}
