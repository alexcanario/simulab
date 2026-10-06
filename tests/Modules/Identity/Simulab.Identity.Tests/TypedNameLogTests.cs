using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-55: no log entry, of any category at any level, carries a name or address a visitor typed. The host records at
/// Trace, because a developer or an operator may lower <c>Default</c> and the rule must hold then too.
/// </summary>
public sealed class TypedNameLogTests : IdentityApiTests
{
    private readonly RecordingLoggerProvider _logs = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Logging:LogLevel:Default"] = nameof(LogLevel.Trace) }))
            .ConfigureLogging(logging => logging.AddProvider(_logs));

    private void AssertNoEntryContains(params string[] typed)
    {
        _logs.Entries.Should().NotBeEmpty("the recorder must see the host's own entries, or this test proves nothing");
        foreach (var name in typed)
        {
            _logs.Entries.Where(entry => entry.Message.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty($"no entry may carry the typed name {name}");
        }
    }

    // AC1: a correct password.
    [Fact]
    public async Task SignIn_WithTheRightPassword_LogsNoTypedName()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, "certo-55@exemplo.com");

        var signedIn = await SignInFrom.PasswordAsync(client, "203.0.113.55", email, SignUpForm.ValidPassword);

        signedIn.AccessToken.Should().NotBeNullOrWhiteSpace();
        AssertNoEntryContains("certo-55@exemplo.com");
    }

    // AC2: a wrong password and an unknown name.
    [Fact]
    public async Task SignIn_WithAWrongPasswordAndAnUnknownName_LogsNoTypedName()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, "errado-55@exemplo.com");

        (await SignInFrom.PasswordAsync(client, "203.0.113.56", email, "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        (await SignInFrom.PasswordAsync(client, "203.0.113.56", "fantasma-55@exemplo.com", "not-the-password")).Error.Should().Be(IdentityErrorCodes.InvalidCredentials);

        AssertNoEntryContains("errado-55@exemplo.com", "fantasma-55@exemplo.com");
    }

    // AC3: refused by the F-38 limit; the one warning line still names the address.
    [Fact]
    public async Task SignIn_RefusedByTheLimit_LogsNoTypedNameAndKeepsTheWarning()
    {
        var client = Client();
        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.57", IdentityRateLimits.SignInFailedAccountsPer15Minutes, prefix: "limite-55");

        var refused = await SignInFrom.PasswordAsync(client, "203.0.113.57", "recusado-55@exemplo.com", "not-the-password");

        refused.Error.Should().Be(IdentityErrorCodes.SignInRateLimited);
        AssertNoEntryContains("limite-55", "recusado-55@exemplo.com");
        _logs.Entries.Where(entry => entry.Category == typeof(SignInAttempt).FullName && entry.Level == LogLevel.Warning)
            .Should().ContainSingle().Which.Message.Should().Contain("203.0.113.57");
    }

    // AC4: sign-up and a password-reset request.
    [Fact]
    public async Task SignUpAndPasswordReset_LogNoTypedAddress()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory, "cadastro-55@exemplo.com");

        var asked = await client.PostAsJsonAsync("/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), AppJson.Options);
        await client.PostAsJsonAsync("/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest("sem-conta-55@exemplo.com"), AppJson.Options);
        await RunJobsAsync();

        asked.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        AssertNoEntryContains("cadastro-55@exemplo.com", "sem-conta-55@exemplo.com");
    }

}
