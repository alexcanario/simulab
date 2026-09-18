using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Persistence.Configurations;
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

    /// <summary>Required by the Identity user store. Claims, external logins and tokens arrive with F-5, F-6 and F-11.</summary>
    public DbSet<IdentityUserClaim<Guid>> UserClaims => Set<IdentityUserClaim<Guid>>();

    public DbSet<IdentityUserLogin<Guid>> UserLogins => Set<IdentityUserLogin<Guid>>();

    public DbSet<IdentityUserToken<Guid>> UserTokens => Set<IdentityUserToken<Guid>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ConsentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserClaimConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserLoginConfiguration());
        modelBuilder.ApplyConfiguration(new IdentityUserTokenConfiguration());

        // User cannot inherit TenantEntity (declared exception of the profile), so the base context does
        // not reach it. Its filters are declared here, with the same names, and capture the context and
        // not the tenant value — a captured value would leak rows between requests (Simulae TK #184).
        modelBuilder.Entity<User>()
            .HasQueryFilter(TenantFilter, user => user.TenantId == null || user.TenantId == CurrentTenantId)
            .HasQueryFilter(SoftDeleteFilter, user => !user.IsDeleted);
    }
}
