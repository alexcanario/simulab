using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-150 through HTTP: <c>PUT /api/v1/identity/users/{id}/tenant</c>, the tenant in the users list and in
/// the role history (AC4-AC8, AC11, AC12, AC14, AC17, AC21, AC23). The class shares one database, so each
/// test works on accounts of its own.
/// </summary>
public sealed class UserTenantTests : UserTenantApiTests
{
    // AC4.
    [Fact]
    public async Task SetTenant_OnAUserWithNone_Answers200WithIt_AndTheListReturnsIt()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();

        var saved = await ReadUserAsync(await PutTenantAsync(admin, user.Id, tenant));

        saved.Id.Should().Be(user.Id);
        saved.TenantId.Should().Be(tenant);
        (await StoredTenantAsync(user.Id)).Should().Be(tenant);
        (await ListUsersAsync(admin, user.Email!)).Items.Should().ContainSingle().Which.TenantId.Should().Be(tenant);
    }

    // AC5.
    [Fact]
    public async Task SetTenant_ToNull_ClearsIt()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        await StoreTenantAsync(user.Id, Guid.CreateVersion7());

        var saved = await ReadUserAsync(await PutTenantAsync(admin, user.Id, null));

        saved.TenantId.Should().BeNull();
        (await StoredTenantAsync(user.Id)).Should().BeNull();
    }

    // AC6.
    [Fact]
    public async Task SetTenant_TheValueAlreadyStored_Answers200AndWritesNoEntry()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, tenant));
        var entries = await RoleChangeCountAsync(user.Id);

        var again = await ReadUserAsync(await PutTenantAsync(admin, user.Id, tenant));

        again.TenantId.Should().Be(tenant);
        entries.Should().Be(1, "the first change was recorded, which proves the trail is written at all");
        (await RoleChangeCountAsync(user.Id)).Should().Be(entries);
    }

    // AC7.
    [Fact]
    public async Task SetTenant_TheEmptyGuid_Is400TenantInvalid_AndChangesNothing()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, tenant);

        var response = await PutTenantAsync(admin, user.Id, Guid.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.UserTenantInvalid);
        (await StoredTenantAsync(user.Id)).Should().Be(tenant);
        (await RoleChangeCountAsync(user.Id)).Should().Be(0);
    }

    // AC8.
    [Fact]
    public async Task SetTenant_UnknownDeletedOrErasedUser_Is404UserNotFound()
    {
        var admin = await AdminAsync();
        var deleted = await Accounts.CreateAsync(Factory.Services);
        await QueryAsync(async context =>
        {
            context.Users.Remove(await context.Users.SingleAsync(row => row.Id == deleted.Id));
            return await context.SaveChangesAsync();
        });
        var erased = await Accounts.CreateAsync(Factory.Services);
        var session = await TokenClient.SignInAsync(Client(), erased.Email!, Accounts.ValidPassword);
        using (var erase = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/account-erasures")
        {
            Content = JsonContent.Create(new EraseAccountRequest(Accounts.ValidPassword), options: AppJson.Options)
        })
        {
            erase.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            (await Client().SendAsync(erase)).IsSuccessStatusCode.Should().BeTrue();
        }

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id, erased.Id })
        {
            var response = await PutTenantAsync(admin, id, Guid.CreateVersion7());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound, id.ToString());
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.UserNotFound);
        }
    }

    // AC11.
    [Theory]
    [InlineData(IdentityRoles.Student)]
    [InlineData(IdentityRoles.Curator)]
    [InlineData(IdentityRoles.Grader)]
    public async Task SetTenant_WithoutRolesManage_Is403AndChangesNothing_OnAnotherAccountAndOnItsOwn(string role)
    {
        var caller = await Accounts.CreateAsync(Factory.Services, roles: role);
        var client = await Accounts.SignedInAsync(Client(), caller.Email!);
        var other = await Accounts.CreateAsync(Factory.Services);

        foreach (var target in new[] { other.Id, caller.Id })
        {
            var response = await PutTenantAsync(client, target, Guid.CreateVersion7());

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
            (await StoredTenantAsync(target)).Should().BeNull();
        }
    }

    // AC11.
    [Fact]
    public async Task SetTenant_WithoutAToken_Is401()
    {
        var user = await Accounts.CreateAsync(Factory.Services);

        var response = await PutTenantAsync(Client(), user.Id, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredTenantAsync(user.Id)).Should().BeNull();
    }

    // AC12.
    [Fact]
    public async Task SetTenant_BodyWithOtherFields_AppliesOnlyTheTenant()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services, active: false, roles: IdentityRoles.Curator);
        var before = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(row => row.Id == user.Id));
        var rolesBefore = (await ListUsersAsync(admin, user.Email!)).Items.Single().Roles;
        var tenant = Guid.CreateVersion7();
        var body = $$"""
            {"tenantId":"{{tenant}}","roleIds":[],"status":"Active","email":"other@exemplo.com","fullName":"Someone Else","isDeleted":true}
            """;

        var response = await admin.PutAsync(TenantRoute(user.Id), new StringContent(body, Encoding.UTF8, "application/json"));

        (await ReadUserAsync(response)).TenantId.Should().Be(tenant);
        var after = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(row => row.Id == user.Id));
        after.TenantId.Should().Be(tenant);
        after.Email.Should().Be(before.Email);
        after.FullName.Should().Be(before.FullName);
        after.Status.Should().Be(before.Status);
        after.SecurityStamp.Should().Be(before.SecurityStamp, "no session ends because of a tenant change (BR7)");
        after.IsDeleted.Should().BeFalse();
        (await ListUsersAsync(admin, user.Email!)).Items.Single().Roles.Should().BeEquivalentTo(rolesBefore);
    }

    // AC12.
    [Theory]
    [InlineData("""{"tenantId":"not-a-guid"}""")]
    [InlineData("""{"tenantId":42}""")]
    public async Task SetTenant_ATenantThatIsNotAGuid_Is400AndChangesNothing(string body)
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, tenant);
        var valid = await PutTenantAsync(admin, user.Id, tenant);

        var response = await admin.PutAsync(TenantRoute(user.Id), new StringContent(body, Encoding.UTF8, "application/json"));

        valid.StatusCode.Should().Be(HttpStatusCode.OK, "the route itself answers a valid body");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredTenantAsync(user.Id)).Should().Be(tenant);
        (await RoleChangeCountAsync(user.Id)).Should().Be(0);
    }

    // AC14.
    [Fact]
    public async Task ListUsers_WithoutRolesManage_Is403WithNoTenantInTheBody()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, tenant));
        var student = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        var client = await Accounts.SignedInAsync(Client(), student.Email!);

        var response = await client.GetAsync(Users);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(tenant.ToString());
    }

    // AC17.
    [Fact]
    public async Task TenantChange_IsInTheRoleHistory_WithTheUserAndTheTenantsBeforeAndAfter()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, first));
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, second));

        var response = await admin.GetAsync($"{RoleChanges}?userId={user.Id}&ascending=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = (await response.Content.ReadFromJsonAsync<RoleChangePageResponse>(AppJson.Options))!.Items;

        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(entry => entry.Action == RoleChangeActions.UserTenantChanged
            && entry.TargetUser!.Id == user.Id && entry.Role == null);
        entries[0].Removed.Should().BeEmpty();
        entries[0].Added.Should().Equal(new RoleChangeItemResponse(first.ToString(), first.ToString(), false));
        entries[1].Removed.Should().Equal(new RoleChangeItemResponse(first.ToString(), first.ToString(), false));
        entries[1].Added.Should().Equal(new RoleChangeItemResponse(second.ToString(), second.ToString(), false));
        entries[1].OccurredAt.Should().Be(Factory.Clock.GetUtcNow());
        entries[1].Author.Should().NotBeNull();
    }

    // AC21.
    [Fact]
    public async Task LeastPrivilegedRole_HoldingOnlyRolesManage_GetsEveryCallOfTheScreen()
    {
        var admin = await AdminAsync();
        var created = await admin.PostAsJsonAsync(Roles, new SaveRoleRequest($"Access desk {Guid.NewGuid():N}"[..20], [IdentityPermissions.RolesManage]), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var desk = (await created.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!;
        var holder = await Accounts.CreateAsync(Factory.Services);
        (await admin.PutAsJsonAsync($"{Users}/{holder.Id}/roles", new SetUserRolesRequest([desk.Id]), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var client = await Accounts.SignedInAsync(Client(), holder.Email!);
        var target = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);

        (await client.GetAsync(Roles)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"{Users}?page=0&pageSize=25")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"{Users}/{target.Id}/roles", new SetUserRolesRequest([]), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadUserAsync(await PutTenantAsync(client, target.Id, Guid.CreateVersion7()))).Id.Should().Be(target.Id);
    }

    // AC23 (the list); the sign-in and the refresh are in UserTenantTokenTests.
    [Fact]
    public async Task UserWithATenant_IsListed()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, tenant);

        var page = await ListUsersAsync(admin, user.Email!);

        page.Items.Should().ContainSingle().Which.Id.Should().Be(user.Id);
        page.Items.Single().TenantId.Should().Be(tenant);
    }

    // BR6: the action is the one the trail stores, so the export and the history read the same value.
    [Fact]
    public async Task TenantChange_StoresTheUserTenantChangedAction()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);

        await ReadUserAsync(await PutTenantAsync(admin, user.Id, Guid.CreateVersion7()));

        var stored = await QueryAsync(context => context.RoleChanges.AsNoTracking().SingleAsync(change => change.TargetUserId == user.Id));
        stored.Action.Should().Be(RoleChangeAction.UserTenantChanged);
        stored.RoleIds.Should().BeEmpty();
        stored.TenantId.Should().BeNull("the entry stays global, so a global administrator keeps seeing it");
    }
}
