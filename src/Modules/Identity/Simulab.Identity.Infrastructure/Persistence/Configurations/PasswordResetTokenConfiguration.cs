using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("password_reset_tokens", table => table.HasComment(
            "The single-use link that lets someone set a new password without knowing the old one. It is spent on the first success and never replayed."));
        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId)
            .HasComment("Whose account the link belongs to.");
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired()
            .HasComment("The hash of the token that travelled in the link. The token itself is never stored, so a copy of this table does not let anyone in.");
        builder.Property(token => token.ExpiresAt)
            .HasComment("When the link stops working, in UTC.");
        builder.Property(token => token.ConsumedAt)
            .HasComment("When the link was spent, in UTC. Null while it is still usable; set once, so it cannot be used twice.");

        // The hash is what a reset looks up, and two users never share one.
        builder.HasIndex(token => token.TokenHash).HasDatabaseName("ux_password_reset_tokens_hash").IsUnique();
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_password_reset_tokens_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
