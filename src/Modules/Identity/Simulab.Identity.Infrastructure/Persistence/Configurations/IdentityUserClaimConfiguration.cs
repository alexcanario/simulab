using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Required by the Identity user store; filled from F-5 on.</summary>
public sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_claims");
        builder.HasKey(claim => claim.Id);
        builder.HasIndex(claim => claim.UserId).HasDatabaseName("ix_user_claims_user_id");
    }
}
