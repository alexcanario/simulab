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

        builder.ToTable("account_events");
        builder.HasKey(accountEvent => accountEvent.Id);

        builder.Property(accountEvent => accountEvent.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(accountEvent => accountEvent.Method).HasConversion<string>().HasMaxLength(40);
        builder.Property(accountEvent => accountEvent.Reason).HasConversion<string>().HasMaxLength(40);
        builder.Property(accountEvent => accountEvent.IpAddress).HasMaxLength(45);

        builder.HasIndex(accountEvent => accountEvent.CreatedAt).HasDatabaseName("ix_account_events_created_at");
        builder.HasIndex(accountEvent => accountEvent.UserId).HasDatabaseName("ix_account_events_user_id");
        builder.HasIndex(accountEvent => accountEvent.Type).HasDatabaseName("ix_account_events_type");
        builder.HasIndex(accountEvent => accountEvent.IpAddress).HasDatabaseName("ix_account_events_ip_address");
    }
}
