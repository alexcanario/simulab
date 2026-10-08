using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;
using Simulab.Testing;
using StackExchange.Redis;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-54 AC8, BR5: the per-client limits sit on a Redis that cannot be reached. Registration, resend, reset and
/// sign-in go on as if under their limit, and the outage is one error line. The rest of the host keeps the working
/// Redis, so only the limiter is cut off.
/// </summary>
public sealed class RateLimitRedisOutageTests : IdentityApiTests
{
    private const string RegisterRoute = "/api/v1/identity/registrations";
    private const string ResendRoute = "/api/v1/identity/email-verifications/resend";
    private const string ResetRequestRoute = "/api/v1/identity/password-reset-requests";

    private readonly RecordingLoggerProvider _logs = new();

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder
            .ConfigureLogging(logging => logging.AddProvider(_logs))
            .ConfigureServices(services =>
            {
                // The container owns the unreachable client, so it is disposed with the host.
                services.AddKeyedSingleton<IConnectionMultiplexer>(
                    "unreachable",
                    (_, _) => ConnectionMultiplexer.Connect(new ConfigurationOptions
                    {
                        EndPoints = { "localhost:1" },
                        AbortOnConnectFail = false,
                        ConnectTimeout = 200,
                        AsyncTimeout = 500,
                        BacklogPolicy = BacklogPolicy.FailFast,
                    }));
                services.RemoveAll<ClientRateLimiter>();
                services.AddSingleton(provider => ActivatorUtilities.CreateInstance<ClientRateLimiter>(
                    provider,
                    provider.GetRequiredKeyedService<IConnectionMultiplexer>("unreachable")));
            });

    [Fact]
    public async Task Registration_RedisUnreachable_GoesOnPastTheLimit()
    {
        var client = Client();

        for (var i = 0; i <= IdentityRateLimits.RegistrationsPerHour + 1; i++)
        {
            await PostAsync(client, RegisterRoute, SignUpForm.Valid($"sem.redis{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        OutageLines().Should().Be(1, "the outage is one line, not one per call");
    }

    [Fact]
    public async Task Resend_RedisUnreachable_GoesOnPastTheLimit()
    {
        var client = Client();

        for (var i = 0; i <= IdentityRateLimits.ResendsPerHour + 1; i++)
        {
            await PostAsync(client, ResendRoute, new ResendVerificationRequest($"ninguem{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        OutageLines().Should().Be(1);
    }

    [Fact]
    public async Task PasswordReset_RedisUnreachable_GoesOnPastTheLimit()
    {
        var client = Client();

        for (var i = 0; i <= IdentityRateLimits.PasswordResetRequestsPerHour + 1; i++)
        {
            await PostAsync(client, ResetRequestRoute, new RequestPasswordResetRequest($"ninguem{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        OutageLines().Should().Be(1);
    }

    [Fact]
    public async Task SignIn_RedisUnreachable_GoesOnPastTheLimit()
    {
        var client = Client();

        await SignInFrom.FailUnknownNamesAsync(client, "203.0.113.50", IdentityRateLimits.SignInFailedAccountsPer15Minutes + 2);

        OutageLines().Should().Be(1);
    }

    // Review (major): the Api's own Redis registration (Aspire client, default options), pointed at nothing from the
    // start, must not turn the first limited call into a 500.
    [Fact]
    public async Task Registration_RealRedisRegistrationUnreachableFromTheStart_StillAccepts()
    {
        using var cold = new IdentityApiFactory { ConfigureHost = builder => builder.UseSetting("ConnectionStrings:redis", "localhost:1") };
        await cold.PrepareAsync(nameof(RateLimitRedisOutageTests) + "Cold");

        using var client = cold.CreateClient("en");
        await PostAsync(client, RegisterRoute, SignUpForm.Valid("frio@exemplo.com"), HttpStatusCode.Accepted);
    }

    private int OutageLines() =>
        _logs.Entries.Count(entry => entry.Level == LogLevel.Error && entry.Message.Contains("rate limits cannot reach Redis", StringComparison.Ordinal));
}
