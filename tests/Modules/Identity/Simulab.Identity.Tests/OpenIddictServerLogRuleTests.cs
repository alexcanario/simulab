using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-55 AC5: the Identity module keeps the OpenIddict server's category at Warning in code, so a configuration key
/// that tries to lower it, on the prefix or on the full category the library logs under, changes nothing.
/// </summary>
public abstract class OpenIddictServerLogRuleTests(string key) : IdentityApiTests
{
    private readonly RecordingLoggerProvider _logs = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Logging:LogLevel:Default"] = nameof(LogLevel.Trace),
                    [key] = nameof(LogLevel.Trace),
                }))
            .ConfigureLogging(logging => logging.AddProvider(_logs));

    [Fact]
    public async Task SignIn_WritesNoOpenIddictServerEntryBelowWarning()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);

        var signedIn = await SignInFrom.PasswordAsync(client, "203.0.113.58", email, SignUpForm.ValidPassword);

        signedIn.AccessToken.Should().NotBeNullOrWhiteSpace();
        _logs.Entries.Should().NotBeEmpty("the recorder must see the host's own entries, or this test proves nothing");
        _logs.Entries.Where(entry => entry.Category.StartsWith("OpenIddict.Server", StringComparison.Ordinal) && entry.Level < LogLevel.Warning)
            .Should().BeEmpty();
    }
}

public sealed class OpenIddictServerPrefixLogRuleTests() : OpenIddictServerLogRuleTests("Logging:LogLevel:OpenIddict.Server");

public sealed class OpenIddictServerDispatcherLogRuleTests() : OpenIddictServerLogRuleTests("Logging:LogLevel:OpenIddict.Server.OpenIddictServerDispatcher");
