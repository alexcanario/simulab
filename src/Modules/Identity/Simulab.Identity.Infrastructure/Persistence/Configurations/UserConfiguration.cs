using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName).HasMaxLength(AccountLimits.EmailMaxLength);
        builder.Property(user => user.NormalizedUserName).HasMaxLength(AccountLimits.EmailMaxLength);
        builder.Property(user => user.Email).HasMaxLength(AccountLimits.EmailMaxLength).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(AccountLimits.EmailMaxLength).IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(AccountLimits.FullNameMaxLength);
        builder.Property(user => user.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.ConcurrencyStamp).IsConcurrencyToken();

        // BR3: the address identifies the account across every tenant, so the index includes TenantId and
        // treats two nulls as equal — without that, two global accounts could share one address.
        builder.HasIndex(user => new { user.TenantId, user.NormalizedEmail })
            .HasDatabaseName("ux_users_tenant_normalized_email")
            .IsUniquePerTenant();

        builder.HasIndex(user => new { user.TenantId, user.NormalizedUserName })
            .HasDatabaseName("ux_users_tenant_normalized_user_name")
            .IsUniquePerTenant();
    }
}
