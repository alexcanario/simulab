using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-150: what the tenant tests share - the route, an administrator, the stored tenant read and written
/// around the Api, and the <c>tenant_id</c> claim of an access token read the way the Api itself reads it.
/// </summary>
public abstract class UserTenantApiTests : IdentityApiTests
{
    protected const string Users = "/api/v1/identity/users";
    protected const string Roles = "/api/v1/identity/roles";
    protected const string RoleChanges = "/api/v1/identity/role-changes";

    protected static string TenantRoute(Guid userId) => $"{Users}/{userId}/tenant";

    protected async Task<HttpClient> AdminAsync()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        return await Accounts.SignedInAsync(Client(), admin.Email!);
    }

    protected static Task<HttpResponseMessage> PutTenantAsync(HttpClient client, Guid userId, Guid? tenantId) =>
        client.PutAsJsonAsync(TenantRoute(userId), new SetUserTenantRequest(tenantId), AppJson.Options);

    /// <summary>The tenant as the store holds it, read past every query filter.</summary>
    protected Task<Guid?> StoredTenantAsync(Guid userId) =>
        QueryAsync(context => context.Users.IgnoreQueryFilters()
            .Where(user => user.Id == userId).Select(user => user.TenantId).SingleAsync());

    /// <summary>Writes the tenant straight into the row, so a test can start from an account that already has one.</summary>
    protected Task<int> StoreTenantAsync(Guid userId, Guid? tenantId) =>
        QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE identity.users SET tenant_id = {tenantId} WHERE id = {userId}"));

    protected Task<int> RoleChangeCountAsync(Guid userId) =>
        QueryAsync(context => context.RoleChanges.CountAsync(change => change.TargetUserId == userId));

    protected static async Task<UserSummaryResponse> ReadUserAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserSummaryResponse>(AppJson.Options))!;
    }

    /// <summary>The users list, searched by the address, as the back office reads it.</summary>
    protected static async Task<UserPageResponse> ListUsersAsync(HttpClient client, string search)
    {
        var response = await client.GetAsync($"{Users}?search={Uri.EscapeDataString(search)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserPageResponse>(AppJson.Options))!;
    }

    /// <summary>The <c>tenant_id</c> claim of an access token, validated by the Api's own OpenIddict validation.</summary>
    protected async Task<string?> TenantClaimAsync(string accessToken)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var validation = scope.ServiceProvider.GetRequiredService<OpenIddictValidationService>();
        var principal = await validation.ValidateAccessTokenAsync(accessToken);
        return principal.FindFirst(SessionClaims.TenantId)?.Value;
    }

    protected static HttpClient Bearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
