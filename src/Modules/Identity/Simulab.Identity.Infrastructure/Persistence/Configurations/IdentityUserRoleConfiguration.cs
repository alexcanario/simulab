using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Required by the Identity user/role stores: which roles a user has (F-6, BR2).</summary>
public sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_roles");
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
        builder.HasIndex(userRole => userRole.RoleId).HasDatabaseName("ix_user_roles_role_id");
    }
}
