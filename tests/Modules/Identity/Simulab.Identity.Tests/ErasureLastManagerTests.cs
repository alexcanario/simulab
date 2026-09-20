using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-10, BR11 (AC9): erasing the last account able to manage roles is refused, and everything it would
/// have changed is rolled back. Its own class, and so its own database: the rule counts every account,
/// which any other test would change (same reason as <see cref="LastManagerTests"/>).
/// </summary>
public sealed class ErasureLastManagerTests : IdentityApiTests
{
    private const string EraseRoute = "/api/v1/identity/account-erasures";

    [Fact]
    public async Task Erase_ByTheLastActiveManager_IsRefusedAndRollsBack_UntilASecondManagerExists()
    {
        var admin = await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        var client = Client();
        var session = (await SignedInSessions.CreateAsync(client, admin.Email!, SignUpForm.ValidPassword, 1))[0];

        using (var refused = await EraseAsync(client, session, SignUpForm.ValidPassword))
        {
            refused.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.AccountErasureLastManager);
        }

        // Nothing of the erasure survived the rollback: the row, its role and its session are as they were.
        var row = await QueryAsync(context => context.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == admin.Id));
        row.Email.Should().Be(admin.Email);
        row.IsDeleted.Should().BeFalse();
        row.Status.Should().Be(AccountStatus.Active);
        (await QueryAsync(context => context.UserRoles.CountAsync(role => role.UserId == admin.Id))).Should().Be(1);
        (await SignedInSessions.StatusOfAsync(client, session)).Should().Be(HttpStatusCode.OK);
        Emails.Messages.Should().NotContain(message => message.To == admin.Email && message.Subject.Contains("erased", StringComparison.OrdinalIgnoreCase));

        // With a second active manager, the same call goes through.
        await Accounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        using var allowed = await EraseAsync(client, session, SignUpForm.ValidPassword);

        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent, await allowed.Content.ReadAsStringAsync());
        (await QueryAsync(context => context.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == admin.Id))).Status
            .Should().Be(AccountStatus.Erased);
    }

    private static async Task<HttpResponseMessage> EraseAsync(HttpClient client, TokenResponse session, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EraseRoute)
        {
            Content = JsonContent.Create(new EraseAccountRequest(password), options: AppJson.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.SendAsync(request);
    }
}
