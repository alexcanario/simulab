using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("permissions");
        builder.HasKey(permission => permission.Name);

        builder.Property(permission => permission.Name).HasMaxLength(100);
        builder.Property(permission => permission.Description).HasMaxLength(500);
    }
}
