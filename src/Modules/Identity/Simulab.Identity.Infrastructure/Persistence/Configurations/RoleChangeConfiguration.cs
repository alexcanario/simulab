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

        builder.ToTable("role_changes", table => table.HasComment(
            "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked. Read by an admin auditing access."));
        builder.HasKey(change => change.Id);

        builder.Property(change => change.Action).HasConversion<string>().HasMaxLength(40).IsRequired()
            .HasComment("What was done, such as RoleCreated, RoleRenamed, RoleDeleted or UserRolesChanged.");
        builder.Property(change => change.RoleId)
            .HasComment("The role that changed. Null when the change was about a person's roles rather than one role.");
        builder.Property(change => change.RoleName).HasMaxLength(256)
            .HasComment("The role's name when the change happened, kept so the trail still reads after the role is deleted.");
        builder.Property(change => change.TargetUserId)
            .HasComment("Whose roles changed. Null when the change was about the role itself.");
        builder.Property(change => change.NameBefore).HasMaxLength(256)
            .HasComment("The name before a rename. Null for any other action.");
        builder.Property(change => change.NameAfter).HasMaxLength(256)
            .HasComment("The name after a rename. Null for any other action.");

        // BR1 (v2): these two are JSON containers with no property behind them, so no comment can be
        // attached; the data dictionary names their shape instead.
        builder.OwnsMany(change => change.Added, items => items.ToJson("added"));
        builder.OwnsMany(change => change.Removed, items => items.ToJson("removed"));

        builder.Property(change => change.RoleIds)
            .HasComment("Every role the change touched, so a search by role finds it whichever action it was.");

        builder.HasIndex(change => change.CreatedAt).HasDatabaseName("ix_role_changes_created_at");
        builder.HasIndex(change => change.CreatedBy).HasDatabaseName("ix_role_changes_created_by");
        builder.HasIndex(change => change.TargetUserId).HasDatabaseName("ix_role_changes_target_user_id");
        builder.HasIndex(change => change.RoleIds).HasDatabaseName("ix_role_changes_role_ids").HasMethod("gin");
    }
}
