using Microsoft.Extensions.Logging;
using Simulab.Identity.Api;

namespace Simulab.Identity.Tests;

/// <summary>F-38 and F-54: the set of distinct failed account names per client address, in the limiter itself, kept in Redis and shared by every replica.</summary>
public sealed class SignInNameLimitTests : RateLimiterHarness
{
    private const int Limit = IdentityRateLimits.SignInFailedAccountsPer15Minutes;

    private static readonly TimeSpan Window = IdentityRateLimits.SignInWindow;

    private static async Task FillAsync(ClientRateLimiter limiter, string key, int names)
    {
        for (var i = 0; i < names; i++)
        {
            (await limiter.ReserveSignInNameAsync(key, $"name-{i}", Limit, Window)).Allowed.Should().BeTrue();
        }
    }

    // AC1, BR1: the 31st different name is refused, with the time left.
    [Fact]
    public async Task Reserve_ThirtyNames_RefusesTheThirtyFirst()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit);

        var refused = await limiter.ReserveSignInNameAsync("sign-in:a", "one-more", Limit, Window);

        refused.Allowed.Should().BeFalse();
        refused.RetryAfter.Should().Be(Window);
    }

    // AC2 (F-54), BR1, BR2: names spread across two replicas fill one set; the 31st is refused on either.
    [Fact]
    public async Task Reserve_ThirtyNamesSpreadAcrossTwoReplicas_RefusesTheThirtyFirstOnEither()
    {
        var replicas = new[] { NewLimiter(), NewLimiter() };
        for (var i = 0; i < Limit; i++)
        {
            (await replicas[i % 2].ReserveSignInNameAsync("sign-in:a", $"name-{i}", Limit, Window)).Allowed.Should().BeTrue();
        }

        Clock.Advance(TimeSpan.FromMinutes(5));

        foreach (var replica in replicas)
        {
            var refused = await replica.ReserveSignInNameAsync("sign-in:a", "one-more", Limit, Window);
            refused.Allowed.Should().BeFalse();
            refused.RetryAfter.Should().Be(TimeSpan.FromMinutes(10), "the time left in the window");

            (await replica.ReserveSignInNameAsync("sign-in:a", "name-3", Limit, Window)).Allowed
                .Should().BeFalse("a name already in the set is refused while the set is full");
        }
    }

    // BR1: the same name failing again adds nothing.
    [Fact]
    public async Task Reserve_TheSameNameAgain_AddsNothing()
    {
        var limiter = NewLimiter();
        for (var i = 0; i < Limit * 2; i++)
        {
            (await limiter.ReserveSignInNameAsync("sign-in:a", "same@exemplo.com", Limit, Window)).Allowed.Should().BeTrue();
        }

        (await limiter.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue();
    }

    // AC6: another address has its own set.
    [Fact]
    public async Task Reserve_AnotherAddress_IsNotAffected()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit);

        (await limiter.ReserveSignInNameAsync("sign-in:b", "name-0", Limit, Window)).Allowed.Should().BeTrue();
    }

    // AC6: when the window of the address ends, the next attempt is processed.
    [Fact]
    public async Task Reserve_AfterTheWindow_StartsAgain()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit);

        Clock.Advance(Window);

        (await limiter.ReserveSignInNameAsync("sign-in:a", "name-0", Limit, Window)).Allowed.Should().BeTrue();
        (await limiter.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue();
    }

    // AC7, BR5: a success takes only its own name out; two more new names are needed to be refused.
    [Fact]
    public async Task Release_TakesOutOnlyThatName()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit - 1);

        await limiter.ReleaseSignInNameAsync("sign-in:a", "name-5");

        // 28 names are left: two new ones fit (29, 30), the third is refused.
        (await limiter.ReserveSignInNameAsync("sign-in:a", "new-1", Limit, Window)).Allowed.Should().BeTrue();
        (await limiter.ReserveSignInNameAsync("sign-in:a", "new-2", Limit, Window)).Allowed.Should().BeTrue();
        (await limiter.ReserveSignInNameAsync("sign-in:a", "new-3", Limit, Window)).Allowed.Should().BeFalse();
    }

    // AC4 (F-54), BR2: a name added on one replica and released on the other leaves alone, and both see it.
    [Fact]
    public async Task Release_OnTheOtherReplica_TakesOutOnlyThatNameForBoth()
    {
        var first = NewLimiter();
        var second = NewLimiter();
        await FillAsync(first, "sign-in:a", Limit);
        (await second.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeFalse();

        await second.ReleaseSignInNameAsync("sign-in:a", "name-7");

        (await first.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue("one place is free again");
        (await second.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue();
        (await first.ReserveSignInNameAsync("sign-in:a", "name-8", Limit, Window)).Allowed.Should().BeTrue("name-8 never left, and the set is not full");
        (await second.ReserveSignInNameAsync("sign-in:a", "new-1", Limit, Window)).Allowed.Should().BeTrue("name-7's place is taken by a new name");
        (await first.ReserveSignInNameAsync("sign-in:a", "new-2", Limit, Window)).Allowed.Should().BeFalse("only name-7 left, so one place was free: the set is full again");
    }

    // BR3: CheckSignInAsync is what the code step reads before the challenge, without adding anything.
    [Fact]
    public async Task RetryAfter_IsSetOnlyWhileTheSetIsFull()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit - 1);
        (await limiter.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue();

        await limiter.ReserveSignInNameAsync("sign-in:a", "last", Limit, Window);
        Clock.Advance(TimeSpan.FromMinutes(5));

        (await limiter.CheckSignInAsync("sign-in:a", Limit)).Should()
            .Match<SignInReservation>(check => !check.Allowed && check.RetryAfter == TimeSpan.FromMinutes(10));
    }

    // AC11 (F-38), BR7: the first refusal of the window is the only one that says so; filling the set does not.
    [Fact]
    public async Task Reserve_ReportsTheFirstRefusalOnceAWindow()
    {
        var limiter = NewLimiter();
        var reached = 0;
        for (var i = 0; i < Limit + 5; i++)
        {
            if ((await limiter.ReserveSignInNameAsync("sign-in:a", $"name-{i}", Limit, Window)).FirstRefusal)
            {
                reached++;
            }
        }

        reached.Should().Be(1);

        Clock.Advance(Window);
        (await limiter.ReserveSignInNameAsync("sign-in:a", "last", Limit, Window)).FirstRefusal.Should().BeFalse("filling the set is no refusal");
        await FillAsync(limiter, "sign-in:a", Limit - 1);
        (await limiter.ReserveSignInNameAsync("sign-in:a", "refused", Limit, Window)).FirstRefusal.Should().BeTrue("a new window reports again");
    }

    // AC6 (F-54), BR3: refusals landing on both replicas report once for the window, and once more in the next.
    [Fact]
    public async Task Reserve_RefusalsOnBothReplicas_ReportOncePerWindow()
    {
        var replicas = new[] { NewLimiter(), NewLimiter() };
        await FillAsync(replicas[0], "sign-in:a", Limit);

        var reports = 0;
        for (var i = 0; i < 6; i++)
        {
            var decision = await replicas[i % 2].ReserveSignInNameAsync("sign-in:a", $"extra-{i}", Limit, Window);
            decision.Allowed.Should().BeFalse();
            reports += decision.FirstRefusal ? 1 : 0;
            reports += (await replicas[(i + 1) % 2].CheckSignInAsync("sign-in:a", Limit)).FirstRefusal ? 1 : 0;
        }

        reports.Should().Be(1);

        Clock.Advance(Window);
        await FillAsync(replicas[1], "sign-in:a", Limit);
        (await replicas[0].ReserveSignInNameAsync("sign-in:a", "extra", Limit, Window)).FirstRefusal.Should().BeTrue("a new window reports again");
    }

    // Review (major, F-38): a success that briefly fills the set must not spend the window's one report.
    [Fact]
    public async Task Reserve_ASuccessThatFillsTheSetForAMoment_DoesNotSpendTheReport()
    {
        var limiter = NewLimiter();
        await FillAsync(limiter, "sign-in:a", Limit - 1);

        (await limiter.ReserveSignInNameAsync("sign-in:a", "x", Limit, Window)).FirstRefusal.Should().BeFalse();
        await limiter.ReleaseSignInNameAsync("sign-in:a", "x");

        (await limiter.ReserveSignInNameAsync("sign-in:a", "y", Limit, Window)).Allowed.Should().BeTrue();
        (await limiter.ReserveSignInNameAsync("sign-in:a", "z", Limit, Window)).FirstRefusal.Should().BeTrue("the real refusal reports");
    }

    // AC14 (F-38): 40 different names at the same time, at most 30 are let through.
    [Fact]
    public async Task Reserve_FortyAtTheSameTime_LetsAtMostThirtyThrough()
    {
        var limiter = NewLimiter();

        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            limiter.ReserveSignInNameAsync("sign-in:a", $"name-{i}", Limit, Window)));

        results.Count(decision => decision.Allowed).Should().Be(Limit);
    }

    // AC3 (F-54), BR2: one place left, two different names reserved at once on two replicas: exactly one is added.
    [Fact]
    public async Task Reserve_TwoNamesAtOnceOnTwoReplicasWithOnePlaceLeft_AddsExactlyOne()
    {
        for (var round = 0; round < 10; round++)
        {
            var first = NewLimiter();
            var second = NewLimiter();
            var key = $"sign-in:round-{round}";
            await FillAsync(first, key, Limit - 1);

            var decisions = await Task.WhenAll(
                Task.Run(() => first.ReserveSignInNameAsync(key, "left", Limit, Window)),
                Task.Run(() => second.ReserveSignInNameAsync(key, "right", Limit, Window)));

            decisions.Count(decision => decision.Allowed).Should().Be(1, $"round {round}");
        }
    }

    // AC7 (F-54), BR4: Redis never holds the typed name; replicas with one secret hash it the same way, another secret differently.
    [Fact]
    public async Task Reserve_StoresOnlyAKeyedHashOfTheName()
    {
        var first = NewLimiter();
        var second = NewLimiter();
        var other = NewLimiter("another-client-secret");

        await first.ReserveSignInNameAsync("sign-in:one", "Maria@exemplo.com", Limit, Window);
        await second.ReserveSignInNameAsync("sign-in:two", "Maria@exemplo.com", Limit, Window);
        await other.ReserveSignInNameAsync("sign-in:three", "Maria@exemplo.com", Limit, Window);

        var database = Redis.GetDatabase();
        async Task<string> NameFieldAsync(string address) =>
            (await database.HashKeysAsync($"identity:rate-limit:{Keyspace}:sign-in:{address}"))
                .Select(field => field.ToString()).Single(field => field.StartsWith("n:", StringComparison.Ordinal));

        (await NameFieldAsync("one")).Should().Be(await NameFieldAsync("two"), "both replicas hold the same client secret");
        (await NameFieldAsync("three")).Should().NotBe(await NameFieldAsync("one"), "another secret gives another hash");

        foreach (var key in Keys())
        {
            key.ToString().Should().NotContainEquivalentOf("maria");
            var entries = await database.HashGetAllAsync(key);
            entries.Select(entry => entry.Name.ToString() + entry.Value).Should().NotContain(text => text.Contains("maria", StringComparison.OrdinalIgnoreCase));
        }
    }

    // AC8 (F-54), BR5: Redis unreachable, reserve, check and release all allow, and the outage is one error line a minute.
    [Fact]
    public async Task SignInLimit_RedisUnreachable_AllowsEveryStep()
    {
        await using var dead = await UnreachableRedisAsync();
        var limiter = NewLimiter(redis: dead);

        for (var i = 0; i < Limit + 5; i++)
        {
            (await limiter.ReserveSignInNameAsync("sign-in:a", $"name-{i}", Limit, Window)).Allowed.Should().BeTrue();
        }

        (await limiter.CheckSignInAsync("sign-in:a", Limit)).Allowed.Should().BeTrue();
        await limiter.ReleaseSignInNameAsync("sign-in:a", "name-0");

        Logs.Count(entry => entry.Level == LogLevel.Error).Should().Be(1);
    }
}
