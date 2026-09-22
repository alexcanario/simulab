using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("email_verification_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();

        // The hash is what a verification looks up, and two users never share one.
        builder.HasIndex(token => token.TokenHash).HasDatabaseName("ux_email_verification_tokens_hash").IsUnique();
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_email_verification_tokens_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
