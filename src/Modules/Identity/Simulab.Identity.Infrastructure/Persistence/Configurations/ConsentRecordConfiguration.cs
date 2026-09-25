using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("consent_records", table => table.HasComment(
            "Evidence that a person accepted the terms and the privacy policy, with the versions they saw. Legal evidence: erasing an account never takes it along."));
        builder.HasKey(record => record.Id);

        builder.Property(record => record.UserId)
            .HasComment("Who accepted.");
        builder.Property(record => record.TermsVersion).HasMaxLength(40).IsRequired()
            .HasComment("The version of the terms that was shown and accepted.");
        builder.Property(record => record.PrivacyVersion).HasMaxLength(40).IsRequired()
            .HasComment("The version of the privacy policy that was shown and accepted.");
        builder.Property(record => record.DeclaresAdult)
            .HasComment("The person declared they are 18 or older when accepting.");
        builder.Property(record => record.AcceptedAt)
            .HasComment("When they accepted, in UTC.");
        builder.Property(record => record.Locale).HasMaxLength(10).IsRequired()
            .HasComment("The language the documents were shown in, such as pt-BR: it is what they actually read.");
        builder.Property(record => record.IpAddress).HasMaxLength(45)
            .HasComment("The address the acceptance came from, long enough for IPv6. Null when it could not be read.");

        builder.HasIndex(record => record.UserId).HasDatabaseName("ix_consent_records_user_id");

        // B-13 D2: consent is legal evidence, so a hard delete of the user never takes it along.
        builder.HasOne<User>().WithMany().HasForeignKey(record => record.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
