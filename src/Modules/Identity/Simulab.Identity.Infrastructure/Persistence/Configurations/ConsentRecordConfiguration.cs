using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("consent_records");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.TermsVersion).HasMaxLength(40).IsRequired();
        builder.Property(record => record.PrivacyVersion).HasMaxLength(40).IsRequired();
        builder.Property(record => record.Locale).HasMaxLength(10).IsRequired();
        builder.Property(record => record.IpAddress).HasMaxLength(45);

        builder.HasIndex(record => record.UserId).HasDatabaseName("ix_consent_records_user_id");

        // B-13 D2: consent is legal evidence, so a hard delete of the user never takes it along.
        builder.HasOne<User>().WithMany().HasForeignKey(record => record.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
