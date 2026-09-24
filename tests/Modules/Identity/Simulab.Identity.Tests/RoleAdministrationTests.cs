using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-9 through HTTP, as the Web calls it. The tests of this class share one database, so each one works on
/// roles and accounts with names of its own and never asserts a global count. The last-manager rule, which
/// is about a global count, has its own class (<see cref="LastManagerTests"/>).
/// </summary>
public sealed class RoleAdministrationTests : IdentityApiTests
{
    private const string Roles = "/api/v1/identity/roles";
    private const string Users = "/api/v1/identity/users";

    [Fact]
    public async Task Seed_TheThreeSeedRoles_AreSystemRoles()
    {
        _ = Factory.Services;

        var system = await QueryAsync(context => context.Roles.Where(role => role.IsSystem).Select(role => role.Name!).ToListAsync());

        system.Should().BeEquivalentTo(IdentityRoles.All);
    }

    [Fact]
    public async Task ListRoles_AsAdmin_ReturnsFlagPermissionsAndUserCountAndHidesDeletedRoles()
    {
        var admin = await AdminAsync();
        var custom = await CreateAsync(admin, Unique("Reviewer"), IdentityPermissions.RolesManage);
        var holder = await Accounts.CreateAsync(Factory.Services);
        var deletedHolder = await Accounts.CreateAsync(Factory.Services);
        await SetRolesAsync(admin, holder.Id, custom.Id);
        await SetRolesAsync(admin, deletedHolder.Id, custom.Id);
        await SoftDeleteUserAsync(deletedHolder.Id);
        var gone = await CreateAsync(admin, Unique("Gone"));
        (await admin.DeleteAsync($"{Roles}/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var roles = (await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!;

        var listed = roles.Single(role => role.Id == custom.Id);
        listed.IsSystem.Should().BeFalse();
        listed.Permissions.Should().Equal(IdentityPermissions.RolesManage);
        listed.UserCount.Should().Be(1, "a deleted account does not hold roles");
        roles.Single(role => role.Name == IdentityRoles.Admin).IsSystem.Should().BeTrue();
        roles.Should().NotContain(role => role.Id == gone.Id);
    }

    // F-33 BR2: the catalog is the union of every module's names, not this module's list. The back
    // office shows them all, so a Catalog permission has to be assignable to a role from here.
    [Fact]
    public async Task ListPermissions_AsAdmin_ReturnsEveryModulesPermissions()
    {
        var admin = await AdminAsync();

        var permissions = await admin.GetFromJsonAsync<List<PermissionResponse>>("/api/v1/identity/permissions", AppJson.Options);

        var names = permissions!.Select(permission => permission.Name).ToList();
        names.Should().Contain(IdentityPermissions.All);
        names.Should().Contain(CatalogPermissions.All);
    }

    [Fact]
    public async Task CreateRole_ValidNameAndKnownPermissions_SavesACustomRole()
    {
        var admin = await AdminAsync();
        var name = Unique("  Content reviewer");

        var response = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name + "  ", [IdentityPermissions.RolesManage]), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = await response.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options);
        role!.Name.Should().Be(name.Trim());
        role.IsSystem.Should().BeFalse();
        role.Permissions.Should().Equal(IdentityPermissions.RolesManage);
        role.UserCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(" a ")]
    [InlineData("123456789012345678901234567890123456789012345678901")]
    public async Task SaveRole_NameOutOfBounds_IsRefused(string name)
    {
        var admin = await AdminAsync();
        var existing = await CreateAsync(admin, Unique("Renamed"));

        var created = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name, []), AppJson.Options);
        var renamed = await admin.PutAsJsonAsync($"{Roles}/{existing.Id}", new SaveRoleRequest(name, []), AppJson.Options);

        created.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await created.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNameInvalid);
        renamed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await renamed.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNameInvalid);
    }

    [Fact]
    public async Task SaveRole_FiftyCharacterName_IsAccepted()
    {
        var admin = await AdminAsync();
        var name = Unique("R").PadRight(RoleLimits.NameMaxLength, 'x');

        var response = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name, []), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SaveRole_NameTakenIgnoringCaseByActiveDeletedOrSeedRole_IsRefused()
    {
        var admin = await AdminAsync();
        var active = await CreateAsync(admin, Unique("Reviewer"));
        var deleted = await CreateAsync(admin, Unique("Old"));
        (await admin.DeleteAsync($"{Roles}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var other = await CreateAsync(admin, Unique("Other"));

        foreach (var taken in new[] { active.Name.ToLowerInvariant(), deleted.Name.ToUpperInvariant(), "ADMIN" })
        {
            var created = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(taken, []), AppJson.Options);
            var renamed = await admin.PutAsJsonAsync($"{Roles}/{other.Id}", new SaveRoleRequest(taken, []), AppJson.Options);

            created.StatusCode.Should().Be(HttpStatusCode.Conflict, taken);
            CodeOf(await created.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNameTaken);
            renamed.StatusCode.Should().Be(HttpStatusCode.Conflict, taken);
            CodeOf(await renamed.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNameTaken);
        }
    }

    [Fact]
    public async Task UpdateRole_SameNameDifferentCase_RenamesItself()
    {
        var admin = await AdminAsync();
        var role = await CreateAsync(admin, Unique("reviewer"));

        var response = await admin.PutAsJsonAsync($"{Roles}/{role.Id}", new SaveRoleRequest(role.Name.ToUpperInvariant(), []), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!.Name.Should().Be(role.Name.ToUpperInvariant());
    }

    [Fact]
    public async Task SaveRole_UnknownPermission_IsRefusedAndNothingChanges()
    {
        var admin = await AdminAsync();
        var name = Unique("Ghost");
        var existing = await CreateAsync(admin, Unique("Keeps"), IdentityPermissions.RolesManage);

        var created = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name, ["catalog.nothing"]), AppJson.Options);
        var updated = await admin.PutAsJsonAsync($"{Roles}/{existing.Id}", new SaveRoleRequest(existing.Name, ["catalog.nothing"]), AppJson.Options);

        created.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await created.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RolePermissionUnknown);
        updated.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await updated.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RolePermissionUnknown);

        var roles = await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options);
        roles.Should().NotContain(role => role.Name == name);
        roles!.Single(role => role.Id == existing.Id).Permissions.Should().Equal(IdentityPermissions.RolesManage);
    }

    [Fact]
    public async Task UpdateRole_PermissionsChanged_ReachTheHolderAfterTheCacheWindow()
    {
        var admin = await AdminAsync();
        var role = await CreateAsync(admin, Unique("Helpdesk"));
        var holder = await Accounts.CreateAsync(Factory.Services);
        await SetRolesAsync(admin, holder.Id, role.Id);
        var holderClient = await Accounts.SignedInAsync(Client(), holder.Email!);
        (await holderClient.GetAsync(Roles)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var updated = await admin.PutAsJsonAsync($"{Roles}/{role.Id}", new SaveRoleRequest(role.Name, [IdentityPermissions.RolesManage]), AppJson.Options);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.Clock.Advance(TimeSpan.FromSeconds(11));

        (await holderClient.GetAsync(Roles)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemRole_RenameOrDelete_IsRefusedButItsPermissionsChange()
    {
        var admin = await AdminAsync();
        var curator = await RoleNamedAsync(admin, IdentityRoles.Curator);

        var renamed = await admin.PutAsJsonAsync($"{Roles}/{curator.Id}", new SaveRoleRequest("Editor", []), AppJson.Options);
        var deleted = await admin.DeleteAsync($"{Roles}/{curator.Id}");
        var granted = await admin.PutAsJsonAsync($"{Roles}/{curator.Id}", new SaveRoleRequest(IdentityRoles.Curator, [IdentityPermissions.RolesManage]), AppJson.Options);
        var restored = await admin.PutAsJsonAsync($"{Roles}/{curator.Id}", new SaveRoleRequest(IdentityRoles.Curator, []), AppJson.Options);

        renamed.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await renamed.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleSystemRoleProtected);
        deleted.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await deleted.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleSystemRoleProtected);
        granted.StatusCode.Should().Be(HttpStatusCode.OK);
        (await granted.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!.Permissions.Should().Equal(IdentityPermissions.RolesManage);
        restored.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminRole_LosingRolesManage_IsRefused()
    {
        var admin = await AdminAsync();
        var adminRole = await RoleNamedAsync(admin, IdentityRoles.Admin);

        var response = await admin.PutAsJsonAsync($"{Roles}/{adminRole.Id}", new SaveRoleRequest(IdentityRoles.Admin, []), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAdminPermissionRequired);
        // The refusal changes nothing: Admin keeps the seeded set, which is every module's (F-33 BR3).
        (await RoleNamedAsync(admin, IdentityRoles.Admin)).Permissions
            .Should().BeEquivalentTo([.. IdentityPermissions.All, .. CatalogPermissions.All]);
    }

    [Fact]
    public async Task DeleteRole_InUse_IsRefused_ThenWithoutHolders_IsSoftDeletedAndItsNameStaysTaken()
    {
        var admin = await AdminAsync();
        var role = await CreateAsync(admin, Unique("Temporary"));
        var holder = await Accounts.CreateAsync(Factory.Services);
        await SetRolesAsync(admin, holder.Id, role.Id);

        var inUse = await admin.DeleteAsync($"{Roles}/{role.Id}");
        await SetRolesAsync(admin, holder.Id);
        var deleted = await admin.DeleteAsync($"{Roles}/{role.Id}");

        inUse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await inUse.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleInUse);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options)).Should().NotContain(listed => listed.Id == role.Id);
        (await QueryAsync(context => context.Roles.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]).SingleAsync(row => row.Id == role.Id)))
            .IsDeleted.Should().BeTrue("the delete is soft (rule ui-project)");
        var again = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(role.Name, []), AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListUsers_SearchAndRoleFilter_ReturnNonDeletedAccountsOfAnyStatusPagedOnTheServer()
    {
        var admin = await AdminAsync();
        // Random, not a v7 prefix: v7 starts with the timestamp, which accounts created in the same millisecond share.
        var tag = Guid.NewGuid().ToString("N")[..10];
        var role = await CreateAsync(admin, Unique("Filter"));
        var pending = await Accounts.CreateAsync(Factory.Services, $"b.{tag}@exemplo.com", active: false, fullName: "Bruno Pending");
        var active = await Accounts.CreateAsync(Factory.Services, $"a.{tag}@exemplo.com", fullName: "Zeca Active");
        var deleted = await Accounts.CreateAsync(Factory.Services, $"c.{tag}@exemplo.com");
        var byName = await Accounts.CreateAsync(Factory.Services, fullName: $"Carla {tag}");
        await SoftDeleteUserAsync(deleted.Id);
        await SetRolesAsync(admin, active.Id, role.Id);

        var search = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?search={tag.ToUpperInvariant()}", AppJson.Options);
        var filtered = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?roleId={role.Id}", AppJson.Options);
        var paged = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?search={tag}&pageSize=1&page=1", AppJson.Options);
        var sorted = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?search={tag}&sortBy=name&descending=true", AppJson.Options);
        var unknownRole = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?roleId={Guid.NewGuid()}", AppJson.Options);

        search!.Total.Should().Be(3);
        search.Items.Select(user => user.Email).Should().Equal(active.Email, pending.Email, byName.Email);
        search.Items.Single(user => user.Id == pending.Id).Status.Should().Be("Pending");
        search.Items.Single(user => user.Id == active.Id).Status.Should().Be("Active");
        search.Items.Single(user => user.Id == active.Id).Roles.Select(held => held.Name).Should().Equal(role.Name);
        filtered!.Items.Select(user => user.Id).Should().Equal(active.Id);
        paged!.Total.Should().Be(3);
        paged.Items.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
        sorted!.Items.Select(user => user.FullName).Should().Equal("Zeca Active", $"Carla {tag}", "Bruno Pending");
        unknownRole!.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    public async Task ListUsers_SearchWithPatternCharacters_MatchesThemLiterally(string character)
    {
        var admin = await AdminAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var literal = await Accounts.CreateAsync(Factory.Services, fullName: $"Ana{character}{tag}");
        await Accounts.CreateAsync(Factory.Services, fullName: $"AnaX{tag}");

        var page = await admin.GetFromJsonAsync<UserPageResponse>($"{Users}?search={Uri.EscapeDataString($"a{character}{tag}")}", AppJson.Options);

        page!.Items.Select(user => user.Id).Should().Equal([literal.Id], "the character matches only itself, not any text");
    }

    [Fact]
    public async Task UpdateSystemRole_BlankName_IsRefusedAsInvalid()
    {
        var admin = await AdminAsync();
        var curator = await RoleNamedAsync(admin, IdentityRoles.Curator);

        var response = await admin.PutAsJsonAsync($"{Roles}/{curator.Id}", new SaveRoleRequest(" ", []), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNameInvalid);
    }

    [Fact]
    public async Task SetUserRoles_SeveralRoles_ReplacesTheSetAndPermissionsAreTheirUnion()
    {
        var admin = await AdminAsync();
        var curator = await RoleNamedAsync(admin, IdentityRoles.Curator);
        var custom = await CreateAsync(admin, Unique("Ops"), IdentityPermissions.RolesManage);
        var user = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);

        var saved = await admin.PutAsJsonAsync($"{Users}/{user.Id}/roles", new SetUserRolesRequest([curator.Id, custom.Id]), AppJson.Options);

        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        (await saved.Content.ReadFromJsonAsync<UserSummaryResponse>(AppJson.Options))!.Roles.Select(role => role.Id)
            .Should().BeEquivalentTo([curator.Id, custom.Id]);
        var session = await (await Accounts.SignedInAsync(Client(), user.Email!))
            .GetFromJsonAsync<SessionInfoResponse>("/api/v1/identity/session", AppJson.Options);
        session!.Permissions.Should().Equal(IdentityPermissions.RolesManage);

        var emptied = await admin.PutAsJsonAsync($"{Users}/{user.Id}/roles", new SetUserRolesRequest([]), AppJson.Options);
        emptied.StatusCode.Should().Be(HttpStatusCode.OK);
        (await emptied.Content.ReadFromJsonAsync<UserSummaryResponse>(AppJson.Options))!.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownTargets_AnswerNotFoundOrRoleUnknown()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var deletedRole = await CreateAsync(admin, Unique("Deleted"));
        (await admin.DeleteAsync($"{Roles}/{deletedRole.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var noUser = await admin.PutAsJsonAsync($"{Users}/{Guid.NewGuid()}/roles", new SetUserRolesRequest([]), AppJson.Options);
        var noRole = await admin.PutAsJsonAsync($"{Users}/{user.Id}/roles", new SetUserRolesRequest([Guid.NewGuid()]), AppJson.Options);
        var goneRole = await admin.PutAsJsonAsync($"{Users}/{user.Id}/roles", new SetUserRolesRequest([deletedRole.Id]), AppJson.Options);
        var updateMissing = await admin.PutAsJsonAsync($"{Roles}/{Guid.NewGuid()}", new SaveRoleRequest("Whatever", []), AppJson.Options);
        var deleteMissing = await admin.DeleteAsync($"{Roles}/{deletedRole.Id}");

        noUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await noUser.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.UserNotFound);
        noRole.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await noRole.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAssignmentRoleUnknown);
        goneRole.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await goneRole.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAssignmentRoleUnknown);
        updateMissing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await updateMissing.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNotFound);
        deleteMissing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await deleteMissing.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleNotFound);
    }

    [Theory]
    [InlineData("GET", "/api/v1/identity/permissions")]
    [InlineData("GET", Roles)]
    [InlineData("POST", Roles)]
    [InlineData("PUT", Roles + "/0198f0a2-0000-7000-8000-000000000001")]
    [InlineData("DELETE", Roles + "/0198f0a2-0000-7000-8000-000000000001")]
    [InlineData("GET", Users)]
    [InlineData("PUT", Users + "/0198f0a2-0000-7000-8000-000000000001/roles")]
    [InlineData("GET", "/api/v1/identity/role-changes")]
    [InlineData("GET", "/api/v1/identity/role-changes/filters")]
    [InlineData("GET", "/api/v1/identity/account-events")]
    public async Task EveryEndpoint_WithoutRolesManage_IsForbidden(string method, string route)
    {
        var student = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        var client = await Accounts.SignedInAsync(Client(), student.Email!);

        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new SaveRoleRequest("Anything", []), options: AppJson.Options);
        }

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
    }

    private async Task<HttpClient> AdminAsync()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        return await Accounts.SignedInAsync(Client(), admin.Email!);
    }

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<RoleResponse> CreateAsync(HttpClient admin, string name, params string[] permissions)
    {
        var response = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name, permissions), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!;
    }

    private static async Task<RoleResponse> RoleNamedAsync(HttpClient admin, string name) =>
        (await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!.Single(role => role.Name == name);

    private static async Task SetRolesAsync(HttpClient admin, Guid userId, params Guid[] roleIds)
    {
        var response = await admin.PutAsJsonAsync($"{Users}/{userId}/roles", new SetUserRolesRequest(roleIds), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private Task<int> SoftDeleteUserAsync(Guid userId) => QueryAsync(async context =>
    {
        var user = await context.Users.SingleAsync(row => row.Id == userId);
        context.Users.Remove(user);
        return await context.SaveChangesAsync();
    });
}
