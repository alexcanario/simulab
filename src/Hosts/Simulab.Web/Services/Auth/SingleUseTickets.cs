using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// Short-lived relays between a plain endpoint and an interactive page (F-20): what the page needs stays on this
/// server, and only a random id travels in the URL. An entry can be read while the page shows it and is spent at most once.
/// </summary>
public abstract class SingleUseTickets<T>(TimeProvider timeProvider, TimeSpan lifetime)
    where T : class
{
    private readonly ConcurrentDictionary<string, (T Value, DateTimeOffset ExpiresAt)> _entries = new(StringComparer.Ordinal);

    public string Issue(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        _entries[id] = (value, timeProvider.GetUtcNow().Add(lifetime));
        Sweep();
        return id;
    }

    /// <summary>The entry, left in place, while it has not expired.</summary>
    public bool TryPeek(string? id, out T value)
    {
        if (id is not null && _entries.TryGetValue(id, out var entry) && entry.ExpiresAt > timeProvider.GetUtcNow())
        {
            value = entry.Value;
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>The entry, removed, while it has not expired.</summary>
    public bool TryConsume(string? id, out T value)
    {
        if (id is not null && _entries.TryRemove(id, out var entry) && entry.ExpiresAt > timeProvider.GetUtcNow())
        {
            value = entry.Value;
            return true;
        }

        value = null!;
        return false;
    }

    private void Sweep()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var (id, entry) in _entries)
        {
            if (entry.ExpiresAt <= now)
            {
                _entries.TryRemove(id, out _);
            }
        }
    }
}
