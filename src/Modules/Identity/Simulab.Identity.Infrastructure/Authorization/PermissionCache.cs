using System.Collections.Concurrent;

namespace Simulab.Identity.Infrastructure.Authorization;

/// <summary>
/// The 10 second per-user cache behind <see cref="PermissionQueryService"/> (F-6, BR3). A singleton, one
/// per host, so its entries never outlive the host they were computed for - unlike a static field, which
/// would leak between the independent hosts each integration test spins up.
/// </summary>
public sealed class PermissionCache
{
    private readonly ConcurrentDictionary<Guid, (IReadOnlyCollection<string> Permissions, DateTimeOffset ExpiresAt)> _entries = new();

    public bool TryGet(Guid userId, DateTimeOffset now, out IReadOnlyCollection<string> permissions)
    {
        if (_entries.TryGetValue(userId, out var entry) && entry.ExpiresAt > now)
        {
            permissions = entry.Permissions;
            return true;
        }

        permissions = [];
        return false;
    }

    public void Set(Guid userId, IReadOnlyCollection<string> permissions, DateTimeOffset expiresAt) =>
        _entries[userId] = (permissions, expiresAt);
}
