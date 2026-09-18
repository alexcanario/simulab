using System.Collections.Concurrent;

namespace Simulab.Identity.Api;

/// <summary>
/// The per-client limits of BR12: a fixed window per key. It counts by client address, so it never
/// depends on whether an account exists and cannot be used to find one (BR4).
/// </summary>
/// <remarks>
/// In memory and per process, which is what one deployable needs today. A second instance would need a
/// shared counter (Redis, F-5); until then this is the whole limit, not a cache in front of one.
/// </remarks>
public sealed class ClientRateLimiter(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);

    /// <summary>True when the call is allowed and counted; false when the window is full.</summary>
    public bool TryAcquire(string key, int permitLimit, TimeSpan window)
    {
        var now = timeProvider.GetUtcNow();
        var allowed = true;

        _windows.AddOrUpdate(
            key,
            _ => new Window(now, 1),
            (_, current) =>
            {
                if (now - current.StartedAt >= window)
                {
                    return new Window(now, 1);
                }

                if (current.Count >= permitLimit)
                {
                    allowed = false;
                    return current;
                }

                return current with { Count = current.Count + 1 };
            });

        Forget(now, window);
        return allowed;
    }

    /// <summary>Drops windows that have run out, so an address that never comes back stops costing memory.</summary>
    private void Forget(DateTimeOffset now, TimeSpan window)
    {
        foreach (var entry in _windows)
        {
            if (now - entry.Value.StartedAt >= window)
            {
                _windows.TryRemove(entry);
            }
        }
    }

    private sealed record Window(DateTimeOffset StartedAt, int Count);
}
