using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("roles", table => table.HasComment(
            "A named set of permissions given to people, such as Student, Curator or Admin. A deleted role keeps its name taken."));
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name).HasMaxLength(256)
            .HasComment("The role's name, as it is shown and as permissions are granted to it.");
        builder.Property(role => role.NormalizedName).HasMaxLength(256)
            .HasComment("The name upper-cased, which is what the unique index compares.");
        builder.Property(role => role.IsSystem)
            .HasComment("True for a role Simulab seeds and depends on, such as Admin: it cannot be renamed or deleted.");
        builder.Property(role => role.ConcurrencyStamp).IsConcurrencyToken()
            .HasComment("Changes on every save, so two admins editing the same role at once cannot overwrite each other silently.");

        // F-9, BR3: not filtered by IsDeleted on purpose - a deleted role's name stays taken.
        builder.HasIndex(role => role.NormalizedName).HasDatabaseName("ux_roles_normalized_name").IsUnique();
    }
}
