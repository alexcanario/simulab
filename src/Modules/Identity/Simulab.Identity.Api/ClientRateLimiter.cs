using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Simulab.Identity.Api;

/// <summary>
/// The per-client limits of BR12: a fixed window per key. It counts by client address, so it never
/// depends on whether an account exists and cannot be used to find one (BR4). F-38 adds the sign-in limit,
/// which counts the distinct account names that failed from one address instead of the calls.
/// </summary>
/// <remarks>
/// In memory and per process, which is what one deployable needs today. A second instance would need a
/// shared counter (Redis, F-54); until then this is the whole limit, not a cache in front of one. Every
/// window keeps its own length, and the sweep removes an entry by that length, so a short window never
/// ends a long one (F-38 AC13).
/// </remarks>
public sealed class ClientRateLimiter(TimeProvider timeProvider)
{
    private static readonly TimeSpan SweepEvery = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SignInWindow> _signIns = new(StringComparer.Ordinal);

    // F-38 BR2: the names are kept as keyed hashes, with a key made at process start, never as typed.
    private readonly byte[] _nameKey = RandomNumberGenerator.GetBytes(32);
    private long _lastSweepTicks = timeProvider.GetUtcNow().UtcTicks;

    /// <summary>True when the call is allowed and counted; false when the window is full.</summary>
    public bool TryAcquire(string key, int permitLimit, TimeSpan window)
    {
        var now = timeProvider.GetUtcNow();
        var allowed = true;

        _windows.AddOrUpdate(
            key,
            _ => new Window(now, window, 1),
            (_, current) =>
            {
                if (now - current.StartedAt >= current.Length)
                {
                    return new Window(now, window, 1);
                }

                if (current.Count >= permitLimit)
                {
                    allowed = false;
                    return current;
                }

                return current with { Count = current.Count + 1 };
            });

        Forget(now);
        return allowed;
    }

    /// <summary>
    /// F-38 BR1, BR3: adds the account name to the address's set, atomically. Refused (nothing added) while the
    /// set already holds <paramref name="nameLimit"/> names, whatever the name; so parallel calls cannot pass a
    /// check together (AC14).
    /// </summary>
    public SignInReservation ReserveSignInName(string key, string accountName, int nameLimit, TimeSpan window)
    {
        var now = timeProvider.GetUtcNow();
        var hash = Hash(accountName);

        while (true)
        {
            var entry = _signIns.GetOrAdd(key, _ => new SignInWindow(now, window));
            lock (entry)
            {
                if (entry.Removed)
                {
                    continue;
                }

                if (now - entry.StartedAt >= entry.Length)
                {
                    entry.Reset(now, window);
                }

                if (entry.Names.Count >= nameLimit)
                {
                    return new SignInReservation(false, entry.RetryAfter(now), false);
                }

                entry.Names.Add(hash);
                var reachedLimit = entry.Names.Count >= nameLimit && !entry.LimitReported;
                entry.LimitReported |= reachedLimit;
                Forget(now);
                return new SignInReservation(true, TimeSpan.Zero, reachedLimit);
            }
        }
    }

    /// <summary>F-38 BR5: takes only this account's name out of the address's set, if it is there.</summary>
    public void ReleaseSignInName(string key, string accountName)
    {
        if (!_signIns.TryGetValue(key, out var entry))
        {
            return;
        }

        lock (entry)
        {
            entry.Names.Remove(Hash(accountName));
        }
    }

    /// <summary>F-38 BR3: how long until the address may sign in again, or null while its set has room.</summary>
    public TimeSpan? SignInRetryAfter(string key, int nameLimit)
    {
        if (!_signIns.TryGetValue(key, out var entry))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        lock (entry)
        {
            return !entry.Removed && now - entry.StartedAt < entry.Length && entry.Names.Count >= nameLimit
                ? entry.RetryAfter(now)
                : null;
        }
    }

    private string Hash(string accountName) =>
        Convert.ToBase64String(HMACSHA256.HashData(_nameKey, Encoding.UTF8.GetBytes(accountName)));

    /// <summary>
    /// Drops windows that have run out, each by its own length, so an address that never comes back stops
    /// costing memory. At most once a minute: under a spray the sweep must not cost more than the spray.
    /// </summary>
    private void Forget(DateTimeOffset now)
    {
        var last = Interlocked.Read(ref _lastSweepTicks);
        if (now.UtcTicks - last < SweepEvery.Ticks
            || Interlocked.CompareExchange(ref _lastSweepTicks, now.UtcTicks, last) != last)
        {
            return;
        }

        foreach (var entry in _windows)
        {
            if (now - entry.Value.StartedAt >= entry.Value.Length)
            {
                _windows.TryRemove(entry);
            }
        }

        foreach (var entry in _signIns)
        {
            var window = entry.Value;
            if (!Monitor.TryEnter(window))
            {
                continue;
            }

            try
            {
                if (now - window.StartedAt >= window.Length)
                {
                    window.Removed = true;
                    _signIns.TryRemove(entry.Key, out _);
                }
            }
            finally
            {
                Monitor.Exit(window);
            }
        }
    }

    private sealed record Window(DateTimeOffset StartedAt, TimeSpan Length, int Count);

    private sealed class SignInWindow(DateTimeOffset startedAt, TimeSpan length)
    {
        public DateTimeOffset StartedAt { get; private set; } = startedAt;

        public TimeSpan Length { get; private set; } = length;

        public HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        public bool LimitReported { get; set; }

        public bool Removed { get; set; }

        public void Reset(DateTimeOffset now, TimeSpan window)
        {
            StartedAt = now;
            Length = window;
            Names.Clear();
            LimitReported = false;
        }

        public TimeSpan RetryAfter(DateTimeOffset now) => StartedAt + Length - now;
    }
}

/// <summary>
/// What <see cref="ClientRateLimiter.ReserveSignInName"/> decided: whether the name was added, the time
/// left in the window when it was not, and whether this call is the one that filled the address's set (BR7).
/// </summary>
public readonly record struct SignInReservation(bool Allowed, TimeSpan RetryAfter, bool ReachedLimit);
