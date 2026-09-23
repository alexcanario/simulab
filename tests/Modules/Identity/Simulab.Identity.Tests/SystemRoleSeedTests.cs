using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-9, BR1: a seed role created before F-9 (no system flag) becomes a system role on the next start, and
/// its holders and permissions stay as they were. Its own class: it rewrites a seed role's flag.
/// </summary>
public sealed class SystemRoleSeedTests : IdentityApiTests
{
    [Fact]
    public async Task Seed_RoleCreatedBeforeF9_IsMarkedAsSystemAndKeepsItsGrants()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        await QueryAsync(context => context.Database.ExecuteSqlRawAsync("UPDATE identity.roles SET is_system = FALSE"));

        await Factory.Services.EnsureRolesAndPermissionsAsync();

        var (system, holders, grants) = await QueryAsync(async context =>
        {
            var adminRole = await context.Roles.SingleAsync(role => role.Name == IdentityRoles.Admin);
            return (
                await context.Roles.Where(role => role.IsSystem).Select(role => role.Name!).ToListAsync(),
                await context.UserRoles.Where(userRole => userRole.RoleId == adminRole.Id).Select(userRole => userRole.UserId).ToListAsync(),
                await context.RolePermissions.Where(grant => grant.RoleId == adminRole.Id).Select(grant => grant.PermissionName).ToListAsync());
        });

        system.Should().BeEquivalentTo(IdentityRoles.All);
        holders.Should().Equal(admin.Id);
        // F-33 BR3: Admin holds every permission every registered module declares, not only Identity's.
        grants.Should().BeEquivalentTo([.. IdentityPermissions.All, .. CatalogPermissions.All]);
    }
}
