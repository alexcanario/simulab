using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-21: the account event trail. Newest first is the only order read, so <c>created_at</c> is indexed; the
/// account, the event and the address are the filters (BR10). An address is 45 characters, as the consent
/// record's.
/// </summary>
public sealed class AccountEventConfiguration : IEntityTypeConfiguration<AccountEvent>
{
    public void Configure(EntityTypeBuilder<AccountEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("account_events", table => table.HasComment(
            "What happened to an account: sign-ins, password changes, two-factor enrolments. A person reads their own recent activity from it, and it is kept when the account is erased, without naming anyone."));
        builder.HasKey(accountEvent => accountEvent.Id);

        builder.Property(accountEvent => accountEvent.UserId)
            .HasComment("Whose account it happened to. Null when the attempt named no existing account.");
        builder.Property(accountEvent => accountEvent.Type).HasConversion<string>().HasMaxLength(40).IsRequired()
            .HasComment("What happened, such as SignInSucceeded, PasswordChanged or TwoFactorEnabled.");
        builder.Property(accountEvent => accountEvent.Method).HasConversion<string>().HasMaxLength(40)
            .HasComment("How it was done, such as Password or Google. Null when the event has no method.");
        builder.Property(accountEvent => accountEvent.Reason).HasConversion<string>().HasMaxLength(40)
            .HasComment("Why it failed, such as WrongPassword or LockedOut. Null when nothing failed.");
        builder.Property(accountEvent => accountEvent.IpAddress).HasMaxLength(45)
            .HasComment("The address the request came from, long enough for IPv6. Null when it could not be read.");

        builder.HasIndex(accountEvent => accountEvent.CreatedAt).HasDatabaseName("ix_account_events_created_at");
        builder.HasIndex(accountEvent => accountEvent.UserId).HasDatabaseName("ix_account_events_user_id");
        builder.HasIndex(accountEvent => accountEvent.Type).HasDatabaseName("ix_account_events_type");
        builder.HasIndex(accountEvent => accountEvent.IpAddress).HasDatabaseName("ix_account_events_ip_address");
    }
}
