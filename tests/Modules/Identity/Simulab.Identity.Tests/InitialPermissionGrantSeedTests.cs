using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-36 BR10 and AC11: <c>catalog.browse</c> is granted to Student, Curator and Admin only in the start that
/// creates the permission row; a later start never gives back what the roles back office took away. Its own
/// class: it rewrites the seed grants.
/// </summary>
public sealed class InitialPermissionGrantSeedTests : IdentityApiTests
{
    private Task<List<string>> RolesHoldingAsync(string permission) => QueryAsync(context =>
        (from grant in context.RolePermissions
         join role in context.Roles on grant.RoleId equals role.Id
         where grant.PermissionName == permission
         select role.Name!).ToListAsync());

    [Fact]
    public async Task Seed_OnAFreshDatabase_GrantsBrowseToStudentCuratorAndAdminOnly()
    {
        // Touching Services starts the host, which runs the seed this test is about.
        _ = Factory.Services;

        (await RolesHoldingAsync(CatalogPermissions.Browse))
            .Should().BeEquivalentTo([IdentityRoles.Student, IdentityRoles.Curator, IdentityRoles.Admin]);
        (await RolesHoldingAsync(CatalogPermissions.Manage))
            .Should().BeEquivalentTo([IdentityRoles.Admin], "catalog.manage is not part of the initial grants");
    }

    [Fact]
    public async Task Seed_AfterTheStudentLostBrowse_DoesNotGiveItBack_ButAdminKeepsIt()
    {
        _ = Factory.Services;
        await QueryAsync(context => context.Database.ExecuteSqlRawAsync(
            "DELETE FROM identity.role_permissions WHERE permission_name = 'catalog.browse' AND role_id IN (SELECT id FROM identity.roles WHERE name = 'Student')"));

        await Factory.Services.EnsureRolesAndPermissionsAsync();

        (await RolesHoldingAsync(CatalogPermissions.Browse))
            .Should().BeEquivalentTo([IdentityRoles.Curator, IdentityRoles.Admin]);
    }
}
