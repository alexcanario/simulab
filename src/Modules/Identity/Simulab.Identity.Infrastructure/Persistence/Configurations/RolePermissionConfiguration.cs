using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("role_permissions", table => table.HasComment(
            "Which permissions a role has. A row is the grant itself; removing it takes the permission away."));
        builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionName });

        builder.Property(rolePermission => rolePermission.RoleId)
            .HasComment("The role the permission is granted to.");
        builder.Property(rolePermission => rolePermission.PermissionName)
            .HasComment("The permission that is granted.");

        builder.HasOne(rolePermission => rolePermission.Role)
            .WithMany()
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany()
            .HasForeignKey(rolePermission => rolePermission.PermissionName)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
