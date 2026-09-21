using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

/// <summary>F-14: the role change audit trail. Newest first is the only order read, so <c>created_at</c> is indexed.</summary>
public sealed class RoleChangeConfiguration : IEntityTypeConfiguration<RoleChange>
{
    public void Configure(EntityTypeBuilder<RoleChange> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("role_changes");
        builder.HasKey(change => change.Id);

        builder.Property(change => change.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(change => change.RoleName).HasMaxLength(256);
        builder.Property(change => change.NameBefore).HasMaxLength(256);
        builder.Property(change => change.NameAfter).HasMaxLength(256);

        builder.OwnsMany(change => change.Added, items => items.ToJson("added"));
        builder.OwnsMany(change => change.Removed, items => items.ToJson("removed"));

        builder.HasIndex(change => change.CreatedAt).HasDatabaseName("ix_role_changes_created_at");
        builder.HasIndex(change => change.CreatedBy).HasDatabaseName("ix_role_changes_created_by");
        builder.HasIndex(change => change.TargetUserId).HasDatabaseName("ix_role_changes_target_user_id");
        builder.HasIndex(change => change.RoleIds).HasDatabaseName("ix_role_changes_role_ids").HasMethod("gin");
    }
}
