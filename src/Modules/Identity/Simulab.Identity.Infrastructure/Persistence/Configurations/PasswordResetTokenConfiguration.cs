using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("password_reset_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();

        // The hash is what a reset looks up, and two users never share one.
        builder.HasIndex(token => token.TokenHash).HasDatabaseName("ux_password_reset_tokens_hash").IsUnique();
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_password_reset_tokens_user_id");
    }
}
