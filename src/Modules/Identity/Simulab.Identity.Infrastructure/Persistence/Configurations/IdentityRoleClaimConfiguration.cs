using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Required by the Identity role store; not used until a role carries claims of its own.</summary>
public sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("role_claims");
        builder.HasKey(claim => claim.Id);
        builder.HasIndex(claim => claim.RoleId).HasDatabaseName("ix_role_claims_role_id");
        builder.HasOne<Role>().WithMany().HasForeignKey(claim => claim.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
