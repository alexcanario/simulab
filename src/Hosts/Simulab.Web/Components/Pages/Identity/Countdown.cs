namespace Simulab.Web.Components.Pages.Identity;

/// <summary>
/// A button cooldown or a lockout counting down on screen (F-7): ticks once a second so the page can
/// redraw, and gives the value to announce in steps of ten, so a live region speaks a few times instead
/// of on every tick (F-4 accessibility rule).
/// </summary>
public sealed class Countdown(TimeProvider clock, Func<Task> onTick) : IDisposable
{
    private DateTimeOffset? _endsAt;
    private Timer? _ticker;

    /// <summary>Whole seconds left; zero when not running.</summary>
    public int Remaining => _endsAt is null ? 0 : Math.Max(0, (int)Math.Ceiling((_endsAt.Value - clock.GetUtcNow()).TotalSeconds));

    public bool IsRunning => Remaining > 0;

    /// <summary><see cref="Remaining"/> rounded up to the next ten.</summary>
    public int Announced => (int)(Math.Ceiling(Remaining / 10.0) * 10);

    public void Start(TimeSpan duration)
    {
        _endsAt = clock.GetUtcNow().Add(duration);
        _ticker?.Dispose();
        _ticker = new Timer(_ =>
        {
            if (Remaining <= 0)
            {
                _endsAt = null;
                _ticker?.Dispose();
                _ticker = null;
            }

            _ = onTick();
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Dispose() => _ticker?.Dispose();
}
