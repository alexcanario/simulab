using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Persistence.Configurations;
using Simulab.Jobs.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// The Identity module's context: its own schema, its own migrations (ADR-0001, decision 6).
/// It inherits <see cref="ModuleDbContext"/> and not ASP.NET Identity's own context, because the
/// module's conventions — schema, snake_case names, tenant and soft-delete filters, audit — must
/// apply to the identity tables as well. The Identity stores need entity types, not a base class.
/// </summary>
public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "identity";

    protected override string Schema => SchemaName;

    public DbSet<User> Users => Set<User>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Required by the Identity user store. Claims, external logins and tokens arrive with F-5, F-6 and F-11.</summary>
    public DbSet<IdentityUserClaim<Guid>> UserClaims => Set<IdentityUserClaim<Guid>>();

    public DbSet<IdentityUserLogin<Guid>> UserLogins => Set<IdentityUserLogin<Guid>>();

    public DbSet<IdentityUserToken<Guid>> UserTokens => Set<IdentityUserToken<Guid>>();

    /// <summary>F-6: the seed roles (Student, Curator, Admin) and the permission catalog.</summary>
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<IdentityRoleClaim<Guid>> RoleClaims => Set<IdentityRoleClaim<Guid>>();

    public DbSet<IdentityUserRole<Guid>> UserRoles => Set<IdentityUserRole<Guid>>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>F-14: the role change audit trail.</summary>
    public DbSet<RoleChange> RoleChanges => Set<RoleChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ConsentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserClaimConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserLoginConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserTokenConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityRoleClaimConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RoleChangeConfiguration());

        // The OpenIddict client (F-5, decision 2: one confidential client, "simulab-web") lives in this
        // schema too; the module owns its own protocol tables like every other identity table.
        // UseOpenIddict() hardcodes PascalCase table names (same reason the Identity tables above need
        // their own ToTable calls): the naming convention plugin never gets a chance to rename them.
        modelBuilder.UseOpenIddict();
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreApplication>().ToTable("openiddict_applications");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreAuthorization>().ToTable("openiddict_authorizations");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreScope>().ToTable("openiddict_scopes");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreToken>().ToTable("openiddict_tokens");

        // User cannot inherit TenantEntity (declared exception of the profile), so the base context does
        // not reach it. Its filters are declared here, with the same names, and capture the context and
        // not the tenant value — a captured value would leak rows between requests (Simulae TK #184).
        modelBuilder.Entity<User>()
            .HasQueryFilter(TenantFilter, user => user.TenantId == null || user.TenantId == CurrentTenantId)
            .HasQueryFilter(SoftDeleteFilter, user => !user.IsDeleted);

        // F-9, BR5: roles are global (no tenant filter) and soft deleted; a deleted role disappears everywhere.
        modelBuilder.Entity<Role>().HasQueryFilter(SoftDeleteFilter, role => !role.IsDeleted);

        // F-13 BR2: the job table of the `jobs` schema, mapped here so a handler can stage an email in
        // the very transaction that writes its token. JobsDbContext owns the table and its migration.
        modelBuilder.AddJobQueue();
    }
}
