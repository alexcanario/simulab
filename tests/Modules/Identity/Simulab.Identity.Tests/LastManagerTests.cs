using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-9, BR8b: nobody can leave the system without an active account that manages roles. One test in its own
/// class (its own database): the rule is about a global count, which any other test would change.
/// </summary>
public sealed class LastManagerTests : IdentityApiTests
{
    private const string Roles = "/api/v1/identity/roles";

    [Fact]
    public async Task Change_ThatLeavesNoActiveManager_IsRefusedAndNothingChanges_UntilASecondManagerExists()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var client = await Accounts.SignedInAsync(Client(), admin.Email!);
        var roles = (await client.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!;
        var adminRole = roles.Single(role => role.Name == IdentityRoles.Admin);

        // A pending account holding Admin does not count: it cannot sign in.
        await Accounts.CreateAsync(Factory.Services, active: false, roles: IdentityRoles.Admin);

        // 1. The only active manager removes their own roles.
        var selfRemoval = await client.PutAsJsonAsync($"/api/v1/identity/users/{admin.Id}/roles", new SetUserRolesRequest([]), AppJson.Options);
        selfRemoval.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await selfRemoval.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAssignmentLastManager);
        (await RolesOfAsync(client, admin.Id)).Should().Equal(adminRole.Id);

        // 2. They move to a custom role that also manages roles: still one manager, so it is accepted.
        var ops = await client.PostAsJsonAsync(Roles, new SaveRoleRequest("Ops", [IdentityPermissions.RolesManage]), AppJson.Options);
        var opsRole = (await ops.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!;
        var moved = await client.PutAsJsonAsync($"/api/v1/identity/users/{admin.Id}/roles", new SetUserRolesRequest([opsRole.Id]), AppJson.Options);
        moved.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Taking the permission away from that custom role would leave nobody: refused, rolled back.
        var stripped = await client.PutAsJsonAsync($"{Roles}/{opsRole.Id}", new SaveRoleRequest("Ops", []), AppJson.Options);
        stripped.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await stripped.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAssignmentLastManager);
        (await client.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!
            .Single(role => role.Id == opsRole.Id).Permissions.Should().Equal(IdentityPermissions.RolesManage);

        // 4. With a second active manager, the same change succeeds.
        await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var allowed = await client.PutAsJsonAsync($"{Roles}/{opsRole.Id}", new SaveRoleRequest("Ops", []), AppJson.Options);
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<IEnumerable<Guid>> RolesOfAsync(HttpClient client, Guid userId)
    {
        var page = await client.GetFromJsonAsync<UserPageResponse>("/api/v1/identity/users?pageSize=100", AppJson.Options);
        return page!.Items.Single(user => user.Id == userId).Roles.Select(role => role.Id);
    }
}
