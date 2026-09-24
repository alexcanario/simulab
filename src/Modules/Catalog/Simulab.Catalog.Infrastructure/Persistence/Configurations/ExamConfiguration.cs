using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;

namespace Simulab.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// F-34: the exams table. The unique index is over the normalized name **inside the issuing authority**
/// (BR10), so two bodies may each have an "Agente" while one body may not have it twice, whatever the case
/// or the accents. It includes <c>TenantId</c> and is NULLS NOT DISTINCT: catalog rows are global, and
/// without it two null tenants would count as different keys (ADR-0001, decision 8). Deleted rows are in
/// the index too: a deleted exam keeps its name taken (BR10).
/// </summary>
public sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("exams");
        builder.HasKey(exam => exam.Id);

        builder.Property(exam => exam.IssuingAuthorityId).IsRequired();

        builder.Property(exam => exam.Name)
            .HasMaxLength(CatalogLimits.ExamNameMaxLength)
            .IsRequired();

        builder.Property(exam => exam.AssessmentType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(exam => exam.Scope)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(exam => exam.ScopeDetail)
            .HasMaxLength(CatalogLimits.ExamScopeDetailMaxLength);

        // The language code, as SupportedLanguages writes it ("pt-BR"); never a culture object (BR9).
        builder.Property(exam => exam.ContentLanguage)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(exam => exam.NormalizedName)
            .HasMaxLength(CatalogLimits.ExamNameMaxLength)
            .IsRequired();

        // BR12 (v2): the issuing authority cannot be deleted while an exam points at it, and the handler
        // answers that with a 409. Restrict is the database saying the same thing, in case anything ever
        // bypasses it.
        builder.HasOne<IssuingAuthority>()
            .WithMany()
            .HasForeignKey(exam => exam.IssuingAuthorityId)
            .HasConstraintName("fk_exams_issuing_authority")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(exam => new { exam.TenantId, exam.IssuingAuthorityId, exam.NormalizedName })
            .HasDatabaseName("ux_exams_tenant_authority_normalized_name")
            .IsUniquePerTenant();

        // The list filters and sorts by the issuing authority on its own (BR14); the unique index above
        // cannot serve that, because its first column is the tenant.
        builder.HasIndex(exam => exam.IssuingAuthorityId)
            .HasDatabaseName("ix_exams_issuing_authority");
    }
}
