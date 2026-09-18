using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Required by the Identity user store; external logins arrive with F-11.</summary>
public sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_logins");
        builder.HasKey(login => new { login.LoginProvider, login.ProviderKey });
        builder.Property(login => login.LoginProvider).HasMaxLength(128);
        builder.Property(login => login.ProviderKey).HasMaxLength(128);
    }
}
