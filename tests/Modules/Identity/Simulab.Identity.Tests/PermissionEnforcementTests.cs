using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>BR2-BR4: every account starts as Student, and a permission check answers 200 or 403 by the database, never by role name.</summary>
public sealed class PermissionEnforcementTests : IdentityApiTests
{
    [Fact]
    public async Task Migration_SeedsTheThreeRolesAndOnlyAdminHasRolesManage()
    {
        // Touching Services starts the host, which runs the seed this test is about.
        _ = Factory.Services;

        var (roleNames, grantedTo) = await QueryAsync(async context =>
        {
            var names = await context.Roles.Select(r => r.Name!).ToListAsync();
            var permission = await context.Permissions.SingleAsync(p => p.Name == IdentityPermissions.RolesManage);
            var granted = await (
                from rolePermission in context.RolePermissions
                where rolePermission.PermissionName == permission.Name
                join role in context.Roles on rolePermission.RoleId equals role.Id
                select role.Name!)
                .ToListAsync();
            return (names, granted);
        });

        roleNames.Should().BeEquivalentTo(IdentityRoles.All);
        grantedTo.Should().Equal(IdentityRoles.Admin);
    }

    [Fact]
    public async Task SignUp_NewAccount_IsAssignedTheStudentRoleAutomatically()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);

        var roleName = await QueryAsync(async context =>
        {
            var user = await context.Users.SingleAsync(u => u.Email == email);
            var roleId = await context.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).SingleAsync();
            return await context.Roles.Where(r => r.Id == roleId).Select(r => r.Name).SingleAsync();
        });

        roleName.Should().Be(IdentityRoles.Student);
    }

    [Fact]
    public async Task GetRoles_SignedInAsStudent_IsForbidden()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/v1/identity/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
    }

    [Fact]
    public async Task GetRoles_SignedInAsAdmin_ReturnsTheSeedRoleNames()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await GrantAsync(email, IdentityRoles.Admin);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var roles = await client.GetFromJsonAsync<List<RoleResponse>>(
            "/api/v1/identity/roles", Simulab.SharedKernel.Serialization.AppJson.Options);

        roles!.Select(role => role.Name).Should().BeEquivalentTo(IdentityRoles.All);
    }

    [Fact]
    public async Task GetSession_SignedInAsAdmin_IncludesTheGrantedPermission()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await GrantAsync(email, IdentityRoles.Admin);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var session = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/api/v1/identity/session", Simulab.SharedKernel.Serialization.AppJson.Options);

        session!.Permissions.Should().Contain(IdentityPermissions.RolesManage);
    }

    [Fact]
    public async Task GetSession_SignedInAsStudent_HasNoPermissions()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var session = await client.GetFromJsonAsync<SessionInfoResponse>(
            "/api/v1/identity/session", Simulab.SharedKernel.Serialization.AppJson.Options);

        session!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoles_PermissionRevokedAfterTokenIssued_TakesEffectOnceTheCacheExpires()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await GrantAsync(email, IdentityRoles.Admin);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        // Still within the cache window: the token is untouched, but the check must reach the database
        // at least once before this - warm the cache with an authorized call first.
        (await client.GetAsync("/api/v1/identity/roles")).StatusCode.Should().Be(HttpStatusCode.OK);

        await RevokeAsync(email, IdentityRoles.Admin);

        // BR3: within the 10s window the cached answer still allows the call.
        (await client.GetAsync("/api/v1/identity/roles")).StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Clock.Advance(TimeSpan.FromSeconds(11));

        var afterExpiry = await client.GetAsync("/api/v1/identity/roles");
        afterExpiry.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<int> GrantAsync(string email, string roleName) => QueryAsync(async context =>
    {
        var user = await context.Users.SingleAsync(u => u.Email == email);
        var role = await context.Roles.SingleAsync(r => r.Name == roleName);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
        return await context.SaveChangesAsync();
    });

    private Task<int> RevokeAsync(string email, string roleName) => QueryAsync(async context =>
    {
        var user = await context.Users.SingleAsync(u => u.Email == email);
        var role = await context.Roles.SingleAsync(r => r.Name == roleName);
        var assignment = await context.UserRoles.SingleAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        context.UserRoles.Remove(assignment);
        return await context.SaveChangesAsync();
    });
}
