using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>F-38: the per-address limit on <c>/connect/token</c>, through the real pipeline (password step, AC1-AC3, AC5-AC9, AC11, AC14).</summary>
public sealed class SignInRateLimitTests : IdentityApiTests
{
    private const int Limit = IdentityRateLimits.SignInFailedAccountsPer15Minutes;

    private readonly RecordingLoggerProvider _logs = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureLogging(logging => logging.AddProvider(_logs));

    private Task<int> AccountEventCountAsync() => QueryAsync(context => context.AccountEvents.CountAsync());

    private Task<int> FailureCountOfAsync(string email) =>
        QueryAsync(context => context.Users.AsNoTracking().Where(user => user.Email == email).Select(user => user.AccessFailedCount).SingleAsync());

    // AC1, BR1, BR3, BR4: 30 names failed from A; the 31st attempt is refused, and its account is left alone.
    [Fact]
    public async Task Password_ThirtyNamesFailedFromTheAddress_RefusesTheNextWithoutTouchingTheAccount()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.11", Limit);
        var eventsBefore = await AccountEventCountAsync();

        var refused = await SignInFrom.PasswordAsync(client, "203.0.113.11", email, "not-the-password");

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        int.Parse(refused.ErrorDescription!, System.Globalization.CultureInfo.InvariantCulture).Should().BeInRange(1, (int)IdentityRateLimits.SignInWindow.TotalSeconds);
        (await FailureCountOfAsync(email)).Should().Be(0, "the refused attempt is not counted against the account");
        (await AccountEventCountAsync()).Should().Be(eventsBefore, "a refused attempt writes no account event");
    }

    // AC1: even the right password is refused, before any account work.
    [Fact]
    public async Task Password_AtTheLimit_RefusesTheRightPasswordToo()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.12", Limit);

        var refused = await SignInFrom.PasswordAsync(client, "203.0.113.12", email, SignUpForm.ValidPassword);

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        refused.AccessToken.Should().BeNull();
    }

    // AC2, BR1, UC2: one name failing again and again is one name; another account keeps signing in.
    [Fact]
    public async Task Password_OneAccountFailingManyTimes_DoesNotBlockTheOthersOnTheSameNetwork()
    {
        var client = Client();
        var student = await ActiveUser.CreateAsync(client, Factory);
        var classmate = await ActiveUser.CreateAsync(client, Factory);

        for (var attempt = 0; attempt < 40; attempt++)
        {
            await SignInFrom.PasswordAsync(client, "203.0.113.13", student, "not-the-password");
        }

        var signedIn = await SignInFrom.PasswordAsync(client, "203.0.113.13", classmate, SignUpForm.ValidPassword);

        signedIn.Error.Should().BeNull(signedIn.ErrorDescription);
        signedIn.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    // AC3, BR1: unknown names, a wrong password and a locked account all count, one name each.
    [Fact]
    public async Task Password_UnknownWrongAndLockedNames_AllCountTowardsTheLimit()
    {
        var client = Client();
        var wrong = await ActiveUser.CreateAsync(client, Factory);
        var locked = await ActiveUser.CreateAsync(client, Factory);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, locked, "not-the-password");
        }

        (await SignInFrom.PasswordAsync(client, "203.0.113.14", wrong, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.14", locked, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.AccountLocked);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.14", Limit - 2);

        var refused = await SignInFrom.PasswordAsync(client, "203.0.113.14", "another@exemplo.com", "not-the-password");

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
    }

    // AC5, BR3: the refresh grant is not refused by the limit.
    [Fact]
    public async Task Refresh_AtTheLimit_IsProcessedAsBefore()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await SignInFrom.PasswordAsync(client, "203.0.113.15", email, SignUpForm.ValidPassword);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.15", Limit);

        var refreshed = await SignInFrom.RefreshAsync(client, "203.0.113.15", session.RefreshToken!);

        refreshed.Error.Should().BeNull(refreshed.ErrorDescription);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    // AC6, BR2: another address is processed; when the window of the first ends, so is the first.
    [Fact]
    public async Task Password_OtherAddressAndAfterTheWindow_AreProcessed()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.16", Limit);

        (await SignInFrom.PasswordAsync(client, "198.51.100.16", email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();
        (await SignInFrom.PasswordAsync(client, "203.0.113.16", email, SignUpForm.ValidPassword)).Error.Should().Be(IdentityErrorCodes.SignInRateLimited);

        Factory.Clock.Advance(IdentityRateLimits.SignInWindow);

        (await SignInFrom.PasswordAsync(client, "203.0.113.16", email, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    // AC7, BR5: 29 names failed, one is X; X signs in; only X's name leaves, two new names fit before the refusal.
    [Fact]
    public async Task Password_ASuccessTakesOnlyItsOwnNameOut()
    {
        var client = Client();
        var x = await ActiveUser.CreateAsync(client, Factory);
        (await SignInFrom.PasswordAsync(client, "203.0.113.17", x, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.17", Limit - 2);

        (await SignInFrom.PasswordAsync(client, "203.0.113.17", x, SignUpForm.ValidPassword)).AccessToken.Should().NotBeNullOrWhiteSpace();

        (await SignInFrom.PasswordAsync(client, "203.0.113.17", "new-1@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.17", "new-2@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.17", "new-3@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
    }

    // AC8, BR3: a name already in the set is refused too once the address is at the limit.
    [Fact]
    public async Task Password_AtTheLimit_RefusesANameAlreadyInTheSet()
    {
        var client = Client();
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.18", Limit);

        var again = await SignInFrom.PasswordAsync(client, "203.0.113.18", "ghost-0@exemplo.com", "not-the-password");

        again.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
    }

    // BR1: the name is trimmed and lower-cased, so the same typed name in another case is the same name.
    [Fact]
    public async Task Password_TheSameNameInAnotherCase_IsTheSameName()
    {
        var client = Client();
        for (var attempt = 0; attempt < Limit + 2; attempt++)
        {
            var typed = attempt % 2 == 0 ? "Aluno@Exemplo.com" : " aluno@exemplo.com ";
            (await SignInFrom.PasswordAsync(client, "203.0.113.19", typed, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        }
    }

    // AC9, BR6: a request with the address header and the right secret is counted on that address; a wrong secret is not believed.
    [Fact]
    public async Task Password_AddressWithAWrongSecret_IsCountedOnTheConnectionNotTheHeader()
    {
        var client = Client();
        for (var i = 0; i < Limit; i++)
        {
            await SignInFrom.PasswordAsync(client, "203.0.113.20", $"ghost-{i}@exemplo.com", "not-the-password", secret: "not-the-secret");
        }

        var believed = await SignInFrom.PasswordAsync(client, "203.0.113.20", "ghost-0@exemplo.com", "not-the-password");
        var connection = await SignInFrom.PasswordAsync(client, address: null, "ghost-0@exemplo.com", "not-the-password");

        believed.Error.Should().Be(IdentityErrorCodes.InvalidCredentials, "the forged-header failures were not counted on that address");
        connection.Error.Should().Be(IdentityErrorCodes.SignInRateLimited, "they were counted on the connection's address");
    }

    // AC11, BR7: refused attempts write no account event; one warning line, with the address and no name.
    [Fact]
    public async Task Refusals_WriteNoAccountEventAndOneWarningLineWithTheAddress()
    {
        var client = Client();
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.21", Limit, prefix: "visivel");
        var eventsBefore = await AccountEventCountAsync();

        for (var i = 0; i < 5; i++)
        {
            (await SignInFrom.PasswordAsync(client, "203.0.113.21", $"outro-{i}@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        }

        (await AccountEventCountAsync()).Should().Be(eventsBefore);
        var warnings = _logs.Entries.Where(entry => entry.Category == typeof(SignInAttempt).FullName && entry.Level == LogLevel.Warning).ToList();
        warnings.Should().ContainSingle();
        warnings[0].Message.Should().Contain("203.0.113.21");
        warnings[0].Message.Should().NotContain("visivel").And.NotContain("outro-");
    }

    // AC14, BR2: 40 different names at the same time through the endpoint, exactly 30 are processed.
    [Fact]
    public async Task Password_FortyAtTheSameTime_ProcessesThirtyAndRefusesTheRest()
    {
        var client = Client();

        var answers = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            SignInFrom.PasswordAsync(client, "203.0.113.22", $"paralelo-{i}@exemplo.com", "not-the-password")));

        answers.Count(answer => answer.Error == IdentityErrorCodes.InvalidCredentials).Should().Be(Limit);
        answers.Count(answer => answer.Error == IdentityErrorCodes.SignInRateLimited).Should().Be(40 - Limit);
    }
}
