using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-150: the Grader system role in the back office (AC2, AC3) and what it does not open (AC15).</summary>
public sealed class GraderRoleTests : UserTenantApiTests
{
    private static async Task<RoleResponse> GraderAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!
            .Should().ContainSingle(role => role.Name == IdentityRoles.Grader).Subject;

    // AC2.
    [Fact]
    public async Task Grader_RenameOrDelete_IsRefusedButItsPermissionsChange()
    {
        var admin = await AdminAsync();
        var grader = await GraderAsync(admin);

        var renamed = await admin.PutAsJsonAsync($"{Roles}/{grader.Id}", new SaveRoleRequest("Corrector", [QuestionBankPermissions.Grade]), AppJson.Options);
        var deleted = await admin.DeleteAsync($"{Roles}/{grader.Id}");
        var changed = await admin.PutAsJsonAsync(
            $"{Roles}/{grader.Id}",
            new SaveRoleRequest(IdentityRoles.Grader, [QuestionBankPermissions.Grade, QuestionBankPermissions.Review]),
            AppJson.Options);

        renamed.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await renamed.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleSystemRoleProtected);
        deleted.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await deleted.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleSystemRoleProtected);
        changed.StatusCode.Should().Be(HttpStatusCode.OK, await changed.Content.ReadAsStringAsync());
        (await GraderAsync(admin)).Permissions.Should().BeEquivalentTo([QuestionBankPermissions.Grade, QuestionBankPermissions.Review]);
    }

    // AC3.
    [Fact]
    public async Task GiveGrader_TheUserHoldsIt_TheListShowsIt_AndTheRolesScreenListsItWithGrade()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        var grader = await GraderAsync(admin);

        var saved = await admin.PutAsJsonAsync($"{Users}/{user.Id}/roles", new SetUserRolesRequest([grader.Id]), AppJson.Options);

        (await ReadUserAsync(saved)).Roles.Should().Equal(new UserRoleResponse(grader.Id, IdentityRoles.Grader, true));
        (await ListUsersAsync(admin, user.Email!)).Items.Should().ContainSingle()
            .Which.Roles.Select(role => role.Name).Should().Equal(IdentityRoles.Grader);
        grader.IsSystem.Should().BeTrue();
        grader.Permissions.Should().Equal(QuestionBankPermissions.Grade);
        (await GraderAsync(admin)).UserCount.Should().Be(grader.UserCount + 1, "the class shares one database");
    }

    // AC15.
    [Theory]
    [InlineData("/api/v1/identity/roles")]
    [InlineData("/api/v1/identity/users")]
    [InlineData("/api/v1/identity/role-changes")]
    [InlineData("/api/v1/identity/permissions")]
    [InlineData("/api/v1/question-bank/questions")]
    [InlineData("/api/v1/question-bank/review-queue")]
    public async Task OnlyGrader_AdministrationAuthoringAndReviewRoutes_AreForbidden(string route)
    {
        var grader = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Grader);
        var client = await Accounts.SignedInAsync(Client(), grader.Email!);

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
    }
}
