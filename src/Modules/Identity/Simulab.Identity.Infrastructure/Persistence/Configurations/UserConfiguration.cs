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

        builder.ToTable("users", table => table.HasComment(
            "A person with an account. Erasing an account anonymizes this row rather than removing it, so their attempts stay countable without naming anyone."));
        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName).HasMaxLength(AccountLimits.EmailMaxLength)
            .HasComment("The sign-in name. Simulab signs in by email, so it holds the same address.");
        builder.Property(user => user.NormalizedUserName).HasMaxLength(AccountLimits.EmailMaxLength)
            .HasComment("The sign-in name upper-cased, which is what the unique index compares.");
        builder.Property(user => user.Email).HasMaxLength(AccountLimits.EmailMaxLength).IsRequired()
            .HasComment("The address the person signs in with and receives mail at.");
        builder.Property(user => user.NormalizedEmail).HasMaxLength(AccountLimits.EmailMaxLength).IsRequired()
            .HasComment("The address upper-cased, which is what the unique index compares: one account per address.");
        builder.Property(user => user.FullName).HasMaxLength(AccountLimits.FullNameMaxLength)
            .HasComment("The name the person gave, shown in the app. Cleared when the account is erased.");
        builder.Property(user => user.PreferredLanguage).HasMaxLength(10).IsRequired()
            .HasComment("The language the app and its emails use for this person, such as pt-BR. It is the first source of the culture, before the cookie and the browser.");
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired()
            .HasComment("Where the account stands: PendingVerification, Active or Erased.");
        builder.Property(user => user.ConcurrencyStamp).IsConcurrencyToken()
            .HasComment("Changes on every save, so two people editing the same account at once cannot overwrite each other silently.");
        builder.Property(user => user.EmailConfirmed)
            .HasComment("True once the person followed the verification link. Sign-in is refused until then.");
        builder.Property(user => user.PasswordHash)
            .HasComment("The password, hashed by ASP.NET Identity. The password itself is never stored and cannot be recovered from this.");
        builder.Property(user => user.SecurityStamp)
            .HasComment("Changes whenever the credentials change. Every open session that does not carry the current value is rejected, which is how a password change signs other devices out.");
        builder.Property(user => user.PhoneNumber)
            .HasComment("Not used by Simulab: no feature collects a phone number. The column belongs to ASP.NET Identity.");
        builder.Property(user => user.PhoneNumberConfirmed)
            .HasComment("Not used by Simulab, like the phone number itself.");
        builder.Property(user => user.TwoFactorEnabled)
            .HasComment("True when the person finished enrolling in two-factor sign-in.");
        builder.Property(user => user.LockoutEnabled)
            .HasComment("True when this account can be locked out after failed sign-ins. True for everyone.");
        builder.Property(user => user.LockoutEnd)
            .HasComment("The account refuses sign-in until this instant, after too many failed attempts. Null when it is not locked.");
        builder.Property(user => user.AccessFailedCount)
            .HasComment("Failed sign-in attempts since the last success. It reaches the limit and locks the account, and a success clears it.");

        builder.Property(user => user.EmailVerifiedAt)
            .HasComment("When the person followed the verification link, in UTC. Null while the address is unverified.");
        builder.Property(user => user.IsAdultDeclared)
            .HasComment("The person declared at sign-up that they are 18 or older. Simulab takes the declaration; it does not check an age.");
        builder.Property(user => user.TotpEnabledAt)
            .HasComment("When two-factor enrolment finished, in UTC. Null while it is off.");
        builder.Property(user => user.TotpLastAcceptedStep)
            .HasComment("The last time step accepted by two-factor. A code from that step or earlier is refused, so one code cannot be used twice.");

        // F-11 BR3: base64 of nonce, ciphertext and tag of a 32-character secret is 80 characters.
        builder.Property(user => user.TotpSecretEncrypted).HasMaxLength(AccountLimits.TotpSecretEncryptedMaxLength)
            .HasComment("The two-factor secret, encrypted with the app's key. Losing that key makes every enrolment unusable.");

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
