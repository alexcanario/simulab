using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-29 on the Api: the signed-in account's own Google link — read it, add it, remove it. Every call here is
/// the caller's own account; no route takes a user id, so there is nothing to pass but the token.
/// </summary>
public sealed class GoogleLinkTests : IdentityApiTests
{
    protected override void ConfigureHost(IWebHostBuilder builder) => GoogleTokens.Configure(builder);

    private static string NewEmail() => $"ana.{Guid.CreateVersion7():N}@gmail.com";

    private Task<User?> UserAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email));

    private Task<List<string>> GoogleKeysAsync(Guid userId) =>
        QueryAsync(context => context.UserLogins
            .Where(login => login.UserId == userId && login.LoginProvider == GoogleSignInProtocol.LoginProvider)
            .Select(login => login.ProviderKey)
            .ToListAsync());

    private Task<List<AccountEventType>> EventTypesAsync(Guid userId) =>
        QueryAsync(context => context.AccountEvents.AsNoTracking()
            .Where(accountEvent => accountEvent.UserId == userId)
            .Select(accountEvent => accountEvent.Type)
            .ToListAsync());

    /// <summary>An active account with a password, signed in: what the Security page always starts from.</summary>
    private async Task<(string Email, string Token, Guid UserId)> SignedInAsync(HttpClient client, string? email = null)
    {
        var address = await ActiveUser.CreateAsync(client, Factory, email ?? NewEmail());
        var session = await TokenClient.SignInAsync(client, address, SignUpForm.ValidPassword);
        session.AccessToken.Should().NotBeNull(session.Error);
        var user = await UserAsync(address);
        return (address, session.AccessToken!, user!.Id);
    }

    // AC1: the link is written and the page reads it back with the address.
    [Fact]
    public async Task Link_CheckedTokenAndNoLinkYet_LinksItAndTheStateShowsTheAddress()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);
        var subject = GoogleTokens.NewSubject();
        var googleEmail = NewEmail();

        using var response = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, googleEmail));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await GoogleKeysAsync(userId)).Should().Equal(subject);
        var state = await GoogleLinkApi.StateAsync(client, token);
        state.Linked.Should().BeTrue();
        state.Email.Should().Be(googleEmail);
        state.HasPassword.Should().BeTrue();
    }

    // AC2: the same subject again is the second tab of one round trip, and changes nothing.
    [Fact]
    public async Task Link_SameSubjectTwice_SucceedsAndLeavesOneRow()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);
        var subject = GoogleTokens.NewSubject();
        var googleEmail = NewEmail();

        using var first = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, googleEmail));
        using var second = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, googleEmail));

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent, await second.Content.ReadAsStringAsync());
        (await GoogleKeysAsync(userId)).Should().Equal(subject);
    }

    // AC3: this account already has a different Google account; the one it has is untouched.
    [Fact]
    public async Task Link_AccountLinkedToAnotherSubject_IsRefusedAndKeepsTheFirstLink()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);
        var first = GoogleTokens.NewSubject();
        using (await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(first, NewEmail())))
        {
        }

        using var response = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.GoogleLinkAlreadyLinked);
        // Equal(params string[]) would read the reason as a second expected item, so it goes in a collection.
        (await GoogleKeysAsync(userId)).Should().Equal([first], "the link it already had is not replaced");
    }

    // AC4: the Google account belongs to somebody else; neither account changes.
    [Fact]
    public async Task Link_SubjectOwnedByAnotherAccount_IsRefusedAndNeitherAccountChanges()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var (_, ownerToken, ownerId) = await SignedInAsync(client);
        using (await GoogleLinkApi.LinkAsync(client, ownerToken, GoogleTokens.Issue(subject, NewEmail())))
        {
        }

        var (_, token, userId) = await SignedInAsync(client);

        using var response = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, NewEmail()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.GoogleAccountExists);
        (await GoogleKeysAsync(userId)).Should().BeEmpty();
        (await GoogleKeysAsync(ownerId)).Should().Equal(subject);
    }

    // AC5, BR4: the account is known already, so the address never has to match.
    [Fact]
    public async Task Link_GoogleAddressDiffersFromTheAccountAddress_IsAccepted()
    {
        var client = Client();
        var (email, token, userId) = await SignedInAsync(client);
        var googleEmail = NewEmail();
        googleEmail.Should().NotBe(email);

        using var response = await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(GoogleTokens.NewSubject(), googleEmail));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await GoogleKeysAsync(userId)).Should().HaveCount(1);
        (await GoogleLinkApi.StateAsync(client, token)).Email.Should().Be(googleEmail);
    }

    // AC6: an unverified Google address is refused exactly as it is at sign-in.
    [Fact]
    public async Task Link_EmailNotVerified_IsRefusedAndLinksNothing()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);

        using var response = await GoogleLinkApi.LinkAsync(
            client, token, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail(), emailVerified: false));

        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.GoogleEmailNotVerified);
        (await GoogleKeysAsync(userId)).Should().BeEmpty();
    }

    // AC12: the right password removes the row, and the password still signs in.
    [Fact]
    public async Task Unlink_RightPassword_RemovesTheRowAndThePasswordStillWorks()
    {
        var client = Client();
        var (email, token, userId) = await SignedInAsync(client);
        using (await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail())))
        {
        }

        using var response = await GoogleLinkApi.UnlinkAsync(client, token, SignUpForm.ValidPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await GoogleKeysAsync(userId)).Should().BeEmpty();
        (await GoogleLinkApi.StateAsync(client, token)).Linked.Should().BeFalse();
        (await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNull();
    }

    // AC13: a wrong password keeps the link, counts the attempt, and locks the account at the limit.
    [Fact]
    public async Task Unlink_WrongPassword_KeepsTheLinkCountsTheAttemptAndLocksAtTheLimit()
    {
        var client = Client();
        var (email, token, userId) = await SignedInAsync(client);
        var subject = GoogleTokens.NewSubject();
        using (await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, NewEmail())))
        {
        }

        var before = (await UserAsync(email))!.AccessFailedCount;

        using var refused = await GoogleLinkApi.UnlinkAsync(client, token, "Wrong#Password1");

        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.GoogleLinkCurrentPasswordInvalid);
        (await GoogleKeysAsync(userId)).Should().Equal(subject);
        (await UserAsync(email))!.AccessFailedCount.Should().Be(before + 1);

        string? lastCode = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var again = await GoogleLinkApi.UnlinkAsync(client, token, "Wrong#Password1");
            lastCode = CodeOf(await again.Content.ReadAsStringAsync());
        }

        lastCode.Should().Be(IdentityErrorCodes.AccountLocked);
        (await UserAsync(email))!.LockoutEnd.Should().NotBeNull();
        (await GoogleKeysAsync(userId)).Should().Equal([subject], "a lockout never removes the link");
    }

    // AC14, BR9: without a password the link is the only way in, so it stays.
    [Fact]
    public async Task Unlink_AccountWithoutAPassword_AnswersPasswordNotSetAndKeepsTheLink()
    {
        var client = Client();
        var subject = GoogleTokens.NewSubject();
        var email = NewEmail();
        await PostAsync(
            client,
            "/api/v1/identity/google-registrations",
            GoogleTokens.Registration(GoogleTokens.Issue(subject, email)),
            HttpStatusCode.Created);
        var session = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));
        session.AccessToken.Should().NotBeNull(session.Error);
        var userId = (await UserAsync(email))!.Id;

        using var response = await GoogleLinkApi.UnlinkAsync(client, session.AccessToken, SignUpForm.ValidPassword);

        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.PasswordNotSet);
        (await GoogleKeysAsync(userId)).Should().Equal(subject);
        (await GoogleLinkApi.StateAsync(client, session.AccessToken)).HasPassword.Should().BeFalse();
    }

    // AC15, BR10's order: no link is answered before the password is looked at, so an account with neither
    // is told nothing about itself.
    [Fact]
    public async Task Unlink_NoLinkAndNoPasswordGiven_Answers204AndReadsNothingElse()
    {
        var client = Client();
        var (email, token, _) = await SignedInAsync(client);
        var before = (await UserAsync(email))!.AccessFailedCount;

        using var response = await GoogleLinkApi.UnlinkAsync(client, token, currentPassword: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await UserAsync(email))!.AccessFailedCount.Should().Be(before, "a missing password is never checked when there is no link");
    }

    // AC16, BR11: after disconnecting, the address alone does not let Google back in.
    [Fact]
    public async Task SignIn_AfterDisconnecting_IsRefusedAndLinksNothing()
    {
        var client = Client();
        var (email, token, userId) = await SignedInAsync(client);
        var subject = GoogleTokens.NewSubject();
        using (await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(subject, email)))
        {
        }

        using (await GoogleLinkApi.UnlinkAsync(client, token, SignUpForm.ValidPassword))
        {
        }

        var grant = await GoogleTokens.GrantAsync(client, GoogleTokens.Issue(subject, email));

        grant.AccessToken.Should().BeNull();
        grant.Error.Should().Be(IdentityErrorCodes.GoogleAccountExists);
        (await GoogleKeysAsync(userId)).Should().BeEmpty();
    }

    // AC20: the three routes are the caller's own account, so none of them answers anonymously.
    [Theory]
    [InlineData("get")]
    [InlineData("link")]
    [InlineData("unlink")]
    public async Task AnyRoute_Anonymous_Is401(string route)
    {
        var client = Client();

        using var response = route switch
        {
            "get" => await GoogleLinkApi.GetAsync(client, accessToken: null),
            "link" => await GoogleLinkApi.LinkAsync(client, null, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail())),
            _ => await GoogleLinkApi.UnlinkAsync(client, null, SignUpForm.ValidPassword),
        };

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC21, BR13: both actions are account events of the account they belong to.
    [Fact]
    public async Task LinkAndUnlink_AreRecordedAsAccountEventsOfThatAccount()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);
        var (_, otherToken, otherId) = await SignedInAsync(client);
        using (await GoogleLinkApi.LinkAsync(client, otherToken, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail())))
        {
        }

        using (await GoogleLinkApi.LinkAsync(client, token, GoogleTokens.Issue(GoogleTokens.NewSubject(), NewEmail())))
        {
        }

        using (await GoogleLinkApi.UnlinkAsync(client, token, SignUpForm.ValidPassword))
        {
        }

        (await EventTypesAsync(userId)).Should().ContainInOrder(AccountEventType.GoogleLinked, AccountEventType.GoogleUnlinked);
        (await EventTypesAsync(otherId)).Should().Contain(AccountEventType.GoogleLinked)
            .And.NotContain(AccountEventType.GoogleUnlinked, "the other account only linked");
    }

    // AC1 and BR12 together: a row written before F-29 carries the provider's name, not an address.
    [Fact]
    public async Task State_LinkWrittenBeforeThisFeature_IsLinkedWithNoAddress()
    {
        var client = Client();
        var (_, token, userId) = await SignedInAsync(client);
        await QueryAsync(async context =>
        {
            GoogleTokens.Link(context, userId, GoogleTokens.NewSubject(), GoogleSignInProtocol.LoginProvider);
            return await context.SaveChangesAsync();
        });

        var state = await GoogleLinkApi.StateAsync(client, token);

        state.Linked.Should().BeTrue();
        state.Email.Should().BeNull("the display name of an old row is the provider, which is not an address");
    }
}
