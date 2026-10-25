using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure;
using Simulab.QuestionBank.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-150 AC1: the start creates the system role Grader and the permission questions.grade, held by Grader and
/// Admin only; a later start changes nothing and never gives the permission back to Grader. Its own class: it
/// rewrites the seed grants. Admin is not the role tested for "never given back": BR3 of F-6 re-grants every
/// permission to Admin at every start.
/// </summary>
public sealed class GraderRoleSeedTests : IdentityApiTests
{
    private Task<List<string>> RolesHoldingAsync(string permission) => QueryAsync(context =>
        (from grant in context.RolePermissions
         join role in context.Roles on grant.RoleId equals role.Id
         where grant.PermissionName == permission
         select role.Name!).ToListAsync());

    [Fact]
    public async Task Seed_OnAFreshDatabase_CreatesGraderAsASystemRoleAndGradeHeldByGraderAndAdminOnly()
    {
        // Touching Services starts the host, which runs the seed this test is about.
        _ = Factory.Services;

        var grader = await QueryAsync(context => context.Roles.SingleOrDefaultAsync(role => role.Name == IdentityRoles.Grader));
        grader.Should().NotBeNull();
        grader!.IsSystem.Should().BeTrue();
        (await QueryAsync(context => context.Permissions.AnyAsync(permission => permission.Name == QuestionBankPermissions.Grade)))
            .Should().BeTrue();
        (await RolesHoldingAsync(QuestionBankPermissions.Grade))
            .Should().BeEquivalentTo([IdentityRoles.Grader, IdentityRoles.Admin]);
    }

    [Fact]
    public async Task Seed_ASecondStart_ChangesNothing_AndDoesNotGiveGradeBackToGrader()
    {
        _ = Factory.Services;
        var rolesBefore = await QueryAsync(context => context.Roles.CountAsync());
        await QueryAsync(context => context.Database.ExecuteSqlRawAsync(
            "DELETE FROM identity.role_permissions WHERE permission_name = 'questions.grade' AND role_id IN (SELECT id FROM identity.roles WHERE name = 'Grader')"));

        await Factory.Services.EnsureRolesAndPermissionsAsync();

        (await QueryAsync(context => context.Roles.CountAsync())).Should().Be(rolesBefore);
        (await QueryAsync(context => context.Roles.CountAsync(role => role.Name == IdentityRoles.Grader))).Should().Be(1);
        (await RolesHoldingAsync(QuestionBankPermissions.Grade)).Should().BeEquivalentTo([IdentityRoles.Admin]);
    }
}
