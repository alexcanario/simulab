using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-14 through HTTP: the back office's changes leave an audit entry, and the history lists them. The tests of
/// this class share one database, so each one reads the trail filtered by roles and users of its own.
/// </summary>
public sealed class RoleChangeTrailTests : IdentityApiTests
{
    private const string Roles = "/api/v1/identity/roles";
    private const string Users = "/api/v1/identity/users";
    private const string RoleChanges = "/api/v1/identity/role-changes";

    // AC1.
    [Fact]
    public async Task CreateRole_RecordsAuthorTimeRoleAndItsPermissions()
    {
        var (admin, adminId) = await AdminAsync();
        await AddPermissionAsync("test.content.edit");
        var name = Unique("Reviewer");

        var role = await CreateAsync(admin, name, IdentityPermissions.RolesManage, "test.content.edit");

        var entry = (await ListAsync(admin, $"?roleId={role.Id}")).Items.Should().ContainSingle().Subject;
        entry.Action.Should().Be(nameof(RoleChangeAction.RoleCreated));
        entry.Author.Should().Be(new RoleChangeUserResponse(adminId, AdminEmailOf(adminId)));
        entry.OccurredAt.Should().Be(Factory.Clock.GetUtcNow());
        entry.Role.Should().Be(new UserRoleResponse(role.Id, name, false));
        entry.TargetUser.Should().BeNull();
        entry.Added.Select(item => item.Key).Should().Equal("identity.roles.manage", "test.content.edit");
        entry.Removed.Should().BeEmpty();
    }

    // AC2.
    [Fact]
    public async Task UpdateRole_RenamedAndPermissionsChangedInOneSave_RecordsOneEntryWithBeforeAndAfter()
    {
        var (admin, _) = await AdminAsync();
        await AddPermissionAsync("test.content.edit");
        var before = Unique("Editor");
        var after = Unique("Senior editor");
        var role = await CreateAsync(admin, before, "test.content.edit");

        var saved = await admin.PutAsJsonAsync($"{Roles}/{role.Id}", new SaveRoleRequest(after, [IdentityPermissions.RolesManage]), AppJson.Options);

        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = (await ListAsync(admin, $"?roleId={role.Id}")).Items;
        entries.Should().HaveCount(2);
        var entry = entries[0];
        entry.Action.Should().Be(nameof(RoleChangeAction.RoleUpdated));
        entry.NameBefore.Should().Be(before);
        entry.NameAfter.Should().Be(after);
        entry.Role!.Name.Should().Be(after);
        entry.Added.Select(item => item.Key).Should().Equal(IdentityPermissions.RolesManage);
        entry.Removed.Select(item => item.Key).Should().Equal("test.content.edit");
    }

    // AC3.
    [Fact]
    public async Task SetUserRoles_RecordsTheTargetAndExactlyTheRolesAddedAndRemoved()
    {
        var (admin, _) = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        var custom = await CreateAsync(admin, Unique("Mentor"));
        var roles = await RolesAsync(admin);

        await SetRolesAsync(admin, user.Id, roles[IdentityRoles.Curator], custom.Id);

        var entry = (await ListAsync(admin, $"?userId={user.Id}")).Items.Should().ContainSingle().Subject;
        entry.Action.Should().Be(nameof(RoleChangeAction.UserRolesChanged));
        entry.TargetUser.Should().Be(new RoleChangeUserResponse(user.Id, user.Email));
        entry.Role.Should().BeNull();
        entry.Added.Should().BeEquivalentTo(
        [
            new RoleChangeItemResponse(roles[IdentityRoles.Curator].ToString(), IdentityRoles.Curator, true),
            new RoleChangeItemResponse(custom.Id.ToString(), custom.Name, false)
        ]);
        entry.Removed.Should().Equal(new RoleChangeItemResponse(roles[IdentityRoles.Student].ToString(), IdentityRoles.Student, true));
    }

    // AC4.
    [Fact]
    public async Task DeleteRole_RecordsItsName()
    {
        var (admin, _) = await AdminAsync();
        var role = await CreateAsync(admin, Unique("Temporary"));

        (await admin.DeleteAsync($"{Roles}/{role.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var entry = (await ListAsync(admin, $"?roleId={role.Id}")).Items[0];
        entry.Action.Should().Be(nameof(RoleChangeAction.RoleDeleted));
        entry.Role!.Name.Should().Be(role.Name);
        entry.Added.Should().BeEmpty();
        entry.Removed.Should().BeEmpty();
    }

    // AC5 (the last-manager refusal has its own database: RoleChangeLastManagerTests).
    [Fact]
    public async Task RefusedOrUnchangedSaves_RecordNothing()
    {
        var (admin, _) = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var role = await CreateAsync(admin, Unique("Busy"));
        var other = await CreateAsync(admin, Unique("Other"));
        await SetRolesAsync(admin, user.Id, role.Id);
        var roles = await RolesAsync(admin);
        var trailBefore = await CountAsync();

        (await admin.PutAsJsonAsync($"{Roles}/{other.Id}", new SaveRoleRequest(role.Name, []), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await admin.DeleteAsync($"{Roles}/{role.Id}")).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await admin.PutAsJsonAsync($"{Roles}/{roles[IdentityRoles.Admin]}", new SaveRoleRequest(IdentityRoles.Admin, []), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await admin.PostAsJsonAsync(Roles, new SaveRoleRequest("x", []), AppJson.Options)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync($"{Roles}/{other.Id}", new SaveRoleRequest(other.Name, []), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK, "an unchanged save is accepted");
        await SetRolesAsync(admin, user.Id, role.Id);

        (await CountAsync()).Should().Be(trailBefore);
    }

    // AC6.
    [Fact]
    public async Task SignUpErasureAndSeed_RecordNothing()
    {
        var (admin, _) = await AdminAsync();
        var client = Client();
        var form = SignUpForm.Valid();
        (await client.PostAsJsonAsync("/api/v1/identity/registrations", form, AppJson.Options)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var signedUp = await QueryAsync(context => context.Users.SingleAsync(user => user.Email == form.Email));
        var erased = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Curator);
        var session = await TokenClient.SignInAsync(Client(), erased.Email!, SignUpForm.ValidPassword);
        using var erase = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/account-erasures")
        {
            Content = JsonContent.Create(new EraseAccountRequest(SignUpForm.ValidPassword), options: AppJson.Options)
        };
        erase.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        (await client.SendAsync(erase)).IsSuccessStatusCode.Should().BeTrue();

        (await ListAsync(admin, $"?userId={signedUp.Id}")).Total.Should().Be(0);
        (await ListAsync(admin, $"?userId={erased.Id}")).Total.Should().Be(0);
        var seedRoles = (await RolesAsync(admin)).Where(role => IdentityRoles.All.Contains(role.Key)).Select(role => role.Value).ToList();
        seedRoles.Should().HaveCount(3);
        (await QueryAsync(context => context.RoleChanges.CountAsync(change =>
            change.Action == RoleChangeAction.RoleCreated && change.RoleId != null && seedRoles.Contains(change.RoleId.Value))))
            .Should().Be(0);
    }

    // AC7.
    [Fact]
    public async Task ListRoleChanges_FiltersCombineAndPageNewestFirst()
    {
        var (first, firstId) = await AdminAsync();
        var (second, secondId) = await AdminAsync();
        var role = await CreateAsync(first, Unique("Filtered"));
        var alice = await Accounts.CreateAsync(Factory.Services, fullName: "Alice Filtro " + Guid.NewGuid().ToString("N")[..6]);
        var bob = await Accounts.CreateAsync(Factory.Services);

        // Access tokens live 15 minutes on the fake clock: sign in again after each jump.
        Factory.Clock.Advance(TimeSpan.FromDays(40));
        second = await Accounts.SignedInAsync(Client(), AdminEmailOf(secondId));
        await SetRolesAsync(second, alice.Id, role.Id);
        Factory.Clock.Advance(TimeSpan.FromDays(20));
        first = await Accounts.SignedInAsync(Client(), AdminEmailOf(firstId));
        second = await Accounts.SignedInAsync(Client(), AdminEmailOf(secondId));
        await SetRolesAsync(first, bob.Id, role.Id);
        await SetRolesAsync(second, alice.Id);

        var byRole = await ListAsync(first, $"?roleId={role.Id}");
        byRole.Items.Select(entry => entry.Action).Should().Equal(
            nameof(RoleChangeAction.UserRolesChanged),
            nameof(RoleChangeAction.UserRolesChanged),
            nameof(RoleChangeAction.UserRolesChanged),
            nameof(RoleChangeAction.RoleCreated));
        byRole.Items.Select(entry => entry.OccurredAt).Should().BeInDescendingOrder();
        (await ListAsync(first, $"?roleId={role.Id}&ascending=true")).Items[0].Action.Should().Be(nameof(RoleChangeAction.RoleCreated));

        (await ListAsync(first, $"?roleId={role.Id}&authorId={secondId}")).Total.Should().Be(2);
        (await ListAsync(first, $"?roleId={role.Id}&authorId={firstId}&days=30")).Items
            .Should().ContainSingle().Which.TargetUser!.Id.Should().Be(bob.Id);
        (await ListAsync(first, $"?roleId={role.Id}&days=30")).Total.Should().Be(3);
        (await ListAsync(first, $"?roleId={role.Id}&days=7")).Total.Should().Be(2);
        (await ListAsync(first, $"?search={Uri.EscapeDataString(alice.FullName![6..])}")).Items
            .Should().OnlyContain(entry => entry.TargetUser!.Id == alice.Id).And.HaveCount(2);
        (await ListAsync(first, $"?userId={alice.Id}&days=7")).Total.Should().Be(1);

        var paged = await ListAsync(first, $"?roleId={role.Id}&page=1&pageSize=3");
        paged.Total.Should().Be(4);
        paged.Items.Should().ContainSingle().Which.Action.Should().Be(nameof(RoleChangeAction.RoleCreated));

        (await ListAsync(first, $"?roleId={Guid.CreateVersion7()}")).Total.Should().Be(0);

        var invalid = await first.GetAsync($"{RoleChanges}?days=5");
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await invalid.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleChangePeriodInvalid);
    }

    // AC8.
    [Fact]
    public async Task ErasedAccountsAndARenamedThenDeletedRole_ReadAsTheyWere()
    {
        var author = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var authorClient = await Accounts.SignedInAsync(Client(), author.Email!);
        var (reader, _) = await AdminAsync();
        var target = await Accounts.CreateAsync(Factory.Services);
        var first = Unique("First name");
        var role = await CreateAsync(authorClient, first);
        await SetRolesAsync(authorClient, target.Id, role.Id);
        await SetRolesAsync(authorClient, target.Id);
        (await authorClient.PutAsJsonAsync($"{Roles}/{role.Id}", new SaveRoleRequest(Unique("Second name"), []), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await authorClient.DeleteAsync($"{Roles}/{role.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await SoftDeleteUserAsync(author.Id);
        await SoftDeleteUserAsync(target.Id);

        var entries = (await ListAsync(reader, $"?roleId={role.Id}&ascending=true")).Items;

        entries.Should().OnlyContain(entry => entry.Author!.Id == author.Id && entry.Author.Email == null);
        entries[0].Role!.Name.Should().Be(first, "the entry keeps the name the role had at that moment");
        entries[1].TargetUser.Should().Be(new RoleChangeUserResponse(target.Id, null));
        entries[1].Added.Single().Name.Should().Be(first);
        var filters = (await reader.GetFromJsonAsync<RoleChangeFiltersResponse>($"{RoleChanges}/filters?userId={target.Id}", AppJson.Options))!;
        filters.Roles.Should().Contain(option => option.Id == role.Id && option.IsDeleted);
        filters.Authors.Should().Contain(new RoleChangeUserResponse(author.Id, null));
        filters.User.Should().Be(new RoleChangeUserResponse(target.Id, null));
    }

    // AC9.
    [Fact]
    public async Task Trail_HasNoWriteRouteAndHoldsNoPersonalData()
    {
        var (admin, _) = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services, fullName: "Beatriz Pessoal");
        await SetRolesAsync(admin, user.Id, (await RolesAsync(admin))[IdentityRoles.Curator]);
        var entry = (await ListAsync(admin, $"?userId={user.Id}")).Items.Single();

        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
        {
            using var request = new HttpRequestMessage(method, method == HttpMethod.Post ? RoleChanges : $"{RoleChanges}/{entry.Id}");
            var response = await admin.SendAsync(request);
            response.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed], method.Method);
        }

        var row = await QueryAsync(context => context.RoleChanges.AsNoTracking().SingleAsync(change => change.Id == entry.Id));
        var stored = JsonSerializer.Serialize(row, AppJson.Options);
        stored.Should().NotContain(user.Email!).And.NotContain("Beatriz");
    }

    private async Task<(HttpClient Client, Guid Id)> AdminAsync()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        _admins[admin.Id] = admin.Email!;
        return (await Accounts.SignedInAsync(Client(), admin.Email!), admin.Id);
    }

    private readonly Dictionary<Guid, string> _admins = [];

    private string AdminEmailOf(Guid id) => _admins[id];

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<RoleResponse> CreateAsync(HttpClient admin, string name, params string[] permissions)
    {
        var response = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest(name, permissions), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!;
    }

    private static async Task<Dictionary<string, Guid>> RolesAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<List<RoleResponse>>(Roles, AppJson.Options))!.ToDictionary(role => role.Name, role => role.Id);

    private static async Task SetRolesAsync(HttpClient admin, Guid userId, params Guid[] roleIds)
    {
        var response = await admin.PutAsJsonAsync($"{Users}/{userId}/roles", new SetUserRolesRequest(roleIds), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<RoleChangePageResponse> ListAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync(RoleChanges + query);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<RoleChangePageResponse>(AppJson.Options))!;
    }

    private Task<int> CountAsync() => QueryAsync(context => context.RoleChanges.CountAsync());

    private Task<int> AddPermissionAsync(string name) => QueryAsync(async context =>
    {
        if (await context.Permissions.AnyAsync(permission => permission.Name == name))
        {
            return 0;
        }

        context.Permissions.Add(new Permission { Name = name });
        return await context.SaveChangesAsync();
    });

    private Task<int> SoftDeleteUserAsync(Guid userId) => QueryAsync(async context =>
    {
        var user = await context.Users.SingleAsync(row => row.Id == userId);
        context.Users.Remove(user);
        return await context.SaveChangesAsync();
    });
}
