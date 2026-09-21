using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-14, AC5: a change refused by the last-manager rule (F-9, BR8b) is rolled back with its audit entry. Its own
/// class (its own database): the refusal needs exactly one active manager.
/// </summary>
public sealed class RoleChangeLastManagerTests : IdentityApiTests
{
    [Fact]
    public async Task LastManagerRefusal_LeavesNoEntry()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var client = await Accounts.SignedInAsync(Client(), admin.Email!);

        var refused = await client.PutAsJsonAsync($"/api/v1/identity/users/{admin.Id}/roles", new SetUserRolesRequest([]), AppJson.Options);

        refused.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.RoleAssignmentLastManager);
        (await QueryAsync(context => context.RoleChanges.CountAsync())).Should().Be(0);
    }
}
