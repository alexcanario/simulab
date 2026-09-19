using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-9, BR8b: two Admins who each remove their own Admin role at the same moment cannot both succeed - the
/// second would leave nobody. Proves the lock around the check, not only the check. Its own class: the rule
/// is about a global count.
/// </summary>
public sealed class ConcurrentLastManagerTests : IdentityApiTests
{
    [Fact]
    public async Task TwoManagersRemovingThemselvesAtOnce_ExactlyOneIsRefused()
    {
        var first = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var second = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var firstClient = await Accounts.SignedInAsync(Client(), first.Email!);
        var secondClient = await Accounts.SignedInAsync(Client(), second.Email!);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var answers = await Task.WhenAll(
                firstClient.PutAsJsonAsync($"/api/v1/identity/users/{first.Id}/roles", new SetUserRolesRequest([]), AppJson.Options),
                secondClient.PutAsJsonAsync($"/api/v1/identity/users/{second.Id}/roles", new SetUserRolesRequest([]), AppJson.Options));

            answers.Select(answer => answer.StatusCode)
                .Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity], $"attempt {attempt + 1}");

            // Give the removed one Admin back, through the one who still holds it, and try again.
            var (kept, removed) = answers[0].StatusCode == HttpStatusCode.OK ? (secondClient, first) : (firstClient, second);
            var adminRoleId = (await kept.GetFromJsonAsync<List<RoleResponse>>("/api/v1/identity/roles", AppJson.Options))!
                .Single(role => role.Name == IdentityRoles.Admin).Id;
            (await kept.PutAsJsonAsync($"/api/v1/identity/users/{removed.Id}/roles", new SetUserRolesRequest([adminRoleId]), AppJson.Options))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            // The removed one's permission cache (10 s) must see the role again before the next round.
            Factory.Clock.Advance(TimeSpan.FromSeconds(11));
        }
    }
}
