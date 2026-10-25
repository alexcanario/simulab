using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-150: the user's own export carries the tenant and its changes (AC18); erasure keeps the tenant (AC10).</summary>
public sealed class UserTenantDataTests : UserTenantApiTests
{
    private static async Task<HttpResponseMessage> PostAsBearerAsync<T>(string route, string accessToken, T body, HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body, options: AppJson.Options) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    // AC18.
    [Fact]
    public async Task Export_HoldsTheCurrentTenantAndTheTenantChange()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await ReadUserAsync(await PutTenantAsync(admin, user.Id, tenant));
        var client = Client();
        var session = await TokenClient.SignInAsync(client, user.Email!, Accounts.ValidPassword);

        using var response = await PostAsBearerAsync("/api/v1/identity/data-exports", session.AccessToken!, new DataExportRequest(Accounts.ValidPassword), client);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var file = JsonSerializer.Deserialize<DataExportResponse>(await response.Content.ReadAsStringAsync(), AppJson.Options)!;
        file.Identity.Account.TenantId.Should().Be(tenant);
        var change = file.Identity.RoleChanges.Should().ContainSingle().Subject;
        change.Action.Should().Be(RoleChangeActions.UserTenantChanged);
        change.Added.Should().Equal(tenant.ToString());
        change.Removed.Should().BeEmpty();
        change.ByYou.Should().BeFalse();
    }

    // AC10.
    [Fact]
    public async Task Erasure_KeepsTheTenant()
    {
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, tenant);
        var client = Client();
        var session = await TokenClient.SignInAsync(client, user.Email!, Accounts.ValidPassword);
        session.AccessToken.Should().NotBeNull(session.ErrorDescription);

        using var response = await PostAsBearerAsync("/api/v1/identity/account-erasures", session.AccessToken!, new EraseAccountRequest(Accounts.ValidPassword), client);

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var row = await QueryAsync(context => context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(row => row.Id == user.Id));
        row.IsDeleted.Should().BeTrue("the account was erased");
        row.TenantId.Should().Be(tenant);
    }
}
