using Microsoft.Extensions.Logging;
using Simulab.Identity.Api;

namespace Simulab.Identity.Tests;

/// <summary>The fixed window behind the per-client half of BR12, now shared through Redis (F-54).</summary>
public sealed class ClientRateLimiterTests : RateLimiterHarness
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    [Fact]
    public async Task TryAcquire_UpToTheLimit_Allows()
    {
        var limiter = NewLimiter();

        for (var i = 0; i < 3; i++)
        {
            (await limiter.TryAcquireAsync("a", 3, Window)).Should().BeTrue();
        }

        (await limiter.TryAcquireAsync("a", 3, Window)).Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquire_CountsEachKeyOnItsOwn()
    {
        var limiter = NewLimiter();
        (await limiter.TryAcquireAsync("a", 1, Window)).Should().BeTrue();

        (await limiter.TryAcquireAsync("b", 1, Window)).Should().BeTrue("another client has its own window");
        (await limiter.TryAcquireAsync("a", 1, Window)).Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquire_AfterTheWindow_StartsAgain()
    {
        var limiter = NewLimiter();
        (await limiter.TryAcquireAsync("a", 1, Window)).Should().BeTrue();
        (await limiter.TryAcquireAsync("a", 1, Window)).Should().BeFalse();

        Clock.Advance(Window);

        (await limiter.TryAcquireAsync("a", 1, Window)).Should().BeTrue();
    }

    // AC1, BR1: the four call limits, each with its calls spread across two replicas.
    [Theory]
    [InlineData("register", IdentityRateLimits.RegistrationsPerHour)]
    [InlineData("resend", IdentityRateLimits.ResendsPerHour)]
    [InlineData("password-reset-request", IdentityRateLimits.PasswordResetRequestsPerHour)]
    [InlineData("password-reset", IdentityRateLimits.PasswordResetsPerHour)]
    public async Task TryAcquire_CallsSpreadAcrossTwoReplicas_RefusesThePastTheLimitOnEither(string scope, int limit)
    {
        var replicas = new[] { NewLimiter(), NewLimiter() };
        var key = $"{scope}:203.0.113.1";

        for (var i = 0; i < limit; i++)
        {
            (await replicas[i % 2].TryAcquireAsync(key, limit, IdentityRateLimits.Window)).Should().BeTrue($"call {i + 1} is within the limit");
        }

        (await replicas[0].TryAcquireAsync(key, limit, IdentityRateLimits.Window)).Should().BeFalse();
        (await replicas[1].TryAcquireAsync(key, limit, IdentityRateLimits.Window)).Should().BeFalse();

        Clock.Advance(IdentityRateLimits.Window);

        (await replicas[1].TryAcquireAsync(key, limit, IdentityRateLimits.Window)).Should().BeTrue("the next window allows again");
    }

    // AC5, BR2 (F-38 AC13): a short window on the same key never ends a long one.
    [Fact]
    public async Task TryAcquire_AShortWindowOnTheSameKey_DoesNotEndTheLongOne()
    {
        var limiter = NewLimiter();
        var quarter = TimeSpan.FromMinutes(15);
        (await limiter.TryAcquireAsync("shared-key", 1, Window)).Should().BeTrue();

        Clock.Advance(quarter);

        (await limiter.TryAcquireAsync("shared-key", 1, quarter)).Should().BeFalse("the window that began as an hour is still running");

        Clock.Advance(Window - quarter);

        (await limiter.TryAcquireAsync("shared-key", 1, quarter)).Should().BeTrue();
    }

    // The hourly registration window survives other calls arriving 20 minutes later, on another replica too.
    [Fact]
    public async Task TryAcquire_AnotherReplicaCallingLater_KeepsAnHourlyWindowUntilItsOwnHourEnds()
    {
        var first = NewLimiter();
        var second = NewLimiter();
        (await first.TryAcquireAsync("register:a", 1, Window)).Should().BeTrue();

        Clock.Advance(TimeSpan.FromMinutes(20));
        await second.ReserveSignInNameAsync("sign-in:b", "name-0", IdentityRateLimits.SignInFailedAccountsPer15Minutes, IdentityRateLimits.SignInWindow);

        (await second.TryAcquireAsync("register:a", 1, Window)).Should().BeFalse("the registration window lasts an hour");

        Clock.Advance(TimeSpan.FromMinutes(40));
        (await second.TryAcquireAsync("register:a", 1, Window)).Should().BeTrue();
    }

    // AC9, BR6: every key is under the prefix, lives no longer than its window, and holds no account name.
    [Fact]
    public async Task Keys_AreUnderThePrefixWithATimeToLiveWithinTheWindow()
    {
        var limiter = NewLimiter();
        await limiter.TryAcquireAsync("register:203.0.113.1", 10, IdentityRateLimits.Window);
        await limiter.ReserveSignInNameAsync("sign-in:203.0.113.1", "maria@exemplo.com", 30, IdentityRateLimits.SignInWindow);

        var keys = Keys().ToList();

        keys.Should().HaveCount(2);
        var database = Redis.GetDatabase();
        foreach (var key in keys)
        {
            key.ToString().Should().StartWith("identity:rate-limit:").And.NotContainEquivalentOf("maria");
            var window = key.ToString().Contains("sign-in:", StringComparison.Ordinal) ? IdentityRateLimits.SignInWindow : IdentityRateLimits.Window;
            var timeToLive = await database.KeyTimeToLiveAsync(key);
            timeToLive.Should().NotBeNull();
            timeToLive!.Value.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(window);
        }
    }

    // AC8, BR5: Redis unreachable, the call proceeds as if under its limit; one error line a minute, not one a call.
    [Fact]
    public async Task TryAcquire_RedisUnreachable_AllowsAndLogsOneErrorAMinute()
    {
        await using var dead = await UnreachableRedisAsync();
        var limiter = NewLimiter(redis: dead);

        for (var i = 0; i < 20; i++)
        {
            (await limiter.TryAcquireAsync("register:a", 1, Window)).Should().BeTrue("a limit that cannot count lets the call through");
        }

        Logs.Count(entry => entry.Level == LogLevel.Error).Should().Be(1);

        Clock.Advance(TimeSpan.FromMinutes(1));
        await limiter.TryAcquireAsync("register:a", 1, Window);

        Logs.Count(entry => entry.Level == LogLevel.Error).Should().Be(2, "the outage is still on a minute later");
    }
}
