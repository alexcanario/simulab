using Microsoft.Extensions.Time.Testing;
using Simulab.Identity.Api;

namespace Simulab.Identity.Tests;

/// <summary>The fixed window behind the per-client half of BR12.</summary>
public class ClientRateLimiterTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));

    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    [Fact]
    public void TryAcquire_UpToTheLimit_Allows()
    {
        var limiter = new ClientRateLimiter(_clock);

        for (var i = 0; i < 3; i++)
        {
            limiter.TryAcquire("a", 3, Window).Should().BeTrue();
        }

        limiter.TryAcquire("a", 3, Window).Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_CountsEachKeyOnItsOwn()
    {
        var limiter = new ClientRateLimiter(_clock);
        limiter.TryAcquire("a", 1, Window).Should().BeTrue();

        limiter.TryAcquire("b", 1, Window).Should().BeTrue("another client has its own window");
        limiter.TryAcquire("a", 1, Window).Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_AfterTheWindow_StartsAgain()
    {
        var limiter = new ClientRateLimiter(_clock);
        limiter.TryAcquire("a", 1, Window).Should().BeTrue();
        limiter.TryAcquire("a", 1, Window).Should().BeFalse();

        _clock.Advance(Window);

        limiter.TryAcquire("a", 1, Window).Should().BeTrue();
    }
}
