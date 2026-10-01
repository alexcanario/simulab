using Microsoft.Extensions.Time.Testing;
using Simulab.Identity.Api;

namespace Simulab.Identity.Tests;

/// <summary>F-38: the set of distinct failed account names per client address, in the limiter itself.</summary>
public class SignInNameLimitTests
{
    private const int Limit = IdentityRateLimits.SignInFailedAccountsPer15Minutes;

    private static readonly TimeSpan Window = IdentityRateLimits.SignInWindow;

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

    private static void Fill(ClientRateLimiter limiter, string key, int names)
    {
        for (var i = 0; i < names; i++)
        {
            limiter.ReserveSignInName(key, $"name-{i}", Limit, Window).Allowed.Should().BeTrue();
        }
    }

    // AC1, BR1: the 31st different name is refused, with the time left.
    [Fact]
    public void Reserve_ThirtyNames_RefusesTheThirtyFirst()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit);

        var refused = limiter.ReserveSignInName("sign-in:a", "one-more", Limit, Window);

        refused.Allowed.Should().BeFalse();
        refused.RetryAfter.Should().Be(Window);
    }

    // BR1: the same name failing again adds nothing.
    [Fact]
    public void Reserve_TheSameNameAgain_AddsNothing()
    {
        var limiter = new ClientRateLimiter(_clock);
        for (var i = 0; i < Limit * 2; i++)
        {
            limiter.ReserveSignInName("sign-in:a", "same@exemplo.com", Limit, Window).Allowed.Should().BeTrue();
        }

        limiter.SignInRetryAfter("sign-in:a", Limit).Should().BeNull();
    }

    // BR1: the name is trimmed and lower-cased by the caller; the limiter keeps what it is given.
    // AC8, BR3: at the limit, a name already in the set is refused too.
    [Fact]
    public void Reserve_AtTheLimit_RefusesANameAlreadyInTheSet()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit);

        limiter.ReserveSignInName("sign-in:a", "name-3", Limit, Window).Allowed.Should().BeFalse();
    }

    // AC6: another address has its own set.
    [Fact]
    public void Reserve_AnotherAddress_IsNotAffected()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit);

        limiter.ReserveSignInName("sign-in:b", "name-0", Limit, Window).Allowed.Should().BeTrue();
    }

    // AC6: when the window of the address ends, the next attempt is processed.
    [Fact]
    public void Reserve_AfterTheWindow_StartsAgain()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit);

        _clock.Advance(Window);

        limiter.ReserveSignInName("sign-in:a", "name-0", Limit, Window).Allowed.Should().BeTrue();
        limiter.SignInRetryAfter("sign-in:a", Limit).Should().BeNull();
    }

    // AC7, BR5: a success takes only its own name out; two more new names are needed to be refused.
    [Fact]
    public void Release_TakesOutOnlyThatName()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit - 1);

        limiter.ReleaseSignInName("sign-in:a", "name-5");

        // 28 names are left: two new ones fit (29, 30), the third is refused.
        limiter.ReserveSignInName("sign-in:a", "new-1", Limit, Window).Allowed.Should().BeTrue();
        limiter.ReserveSignInName("sign-in:a", "new-2", Limit, Window).Allowed.Should().BeTrue();
        limiter.ReserveSignInName("sign-in:a", "new-3", Limit, Window).Allowed.Should().BeFalse();
    }

    // BR3: SignInRetryAfter is what the code step reads before the challenge, without adding anything.
    [Fact]
    public void RetryAfter_IsSetOnlyWhileTheSetIsFull()
    {
        var limiter = new ClientRateLimiter(_clock);
        Fill(limiter, "sign-in:a", Limit - 1);
        limiter.SignInRetryAfter("sign-in:a", Limit).Should().BeNull();

        limiter.ReserveSignInName("sign-in:a", "last", Limit, Window);
        _clock.Advance(TimeSpan.FromMinutes(5));

        limiter.SignInRetryAfter("sign-in:a", Limit).Should().Be(TimeSpan.FromMinutes(10));
    }

    // AC11, BR7: the call that fills the set is the only one that says so, once per window.
    [Fact]
    public void Reserve_ReportsReachingTheLimitOnceAWindow()
    {
        var limiter = new ClientRateLimiter(_clock);
        var reached = 0;
        for (var i = 0; i < Limit + 5; i++)
        {
            if (limiter.ReserveSignInName("sign-in:a", $"name-{i}", Limit, Window).ReachedLimit)
            {
                reached++;
            }
        }

        reached.Should().Be(1);

        _clock.Advance(Window);
        Fill(limiter, "sign-in:a", Limit - 1);
        limiter.ReserveSignInName("sign-in:a", "last", Limit, Window).ReachedLimit.Should().BeTrue("a new window reports again");
    }

    // AC13: the hourly registration window survives a sign-in call that sweeps after 20 minutes.
    [Fact]
    public void Sweep_ByASignInCall_KeepsAnHourlyWindowUntilItsOwnHourEnds()
    {
        var limiter = new ClientRateLimiter(_clock);
        var hour = TimeSpan.FromHours(1);
        limiter.TryAcquire("register:a", 1, hour).Should().BeTrue();

        _clock.Advance(TimeSpan.FromMinutes(20));
        limiter.ReserveSignInName("sign-in:b", "name-0", Limit, Window);

        limiter.TryAcquire("register:a", 1, hour).Should().BeFalse("the registration window lasts an hour");

        _clock.Advance(TimeSpan.FromMinutes(40));
        limiter.TryAcquire("register:a", 1, hour).Should().BeTrue();
    }

    // AC14: 40 different names at the same time, at most 30 are let through.
    [Fact]
    public async Task Reserve_FortyAtTheSameTime_LetsAtMostThirtyThrough()
    {
        var limiter = new ClientRateLimiter(_clock);

        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            Task.Run(() => limiter.ReserveSignInName("sign-in:a", $"name-{i}", Limit, Window).Allowed)));

        results.Count(allowed => allowed).Should().Be(Limit);
    }
}
