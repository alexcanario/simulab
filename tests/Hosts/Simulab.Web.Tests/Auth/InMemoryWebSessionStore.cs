using System.Collections.Concurrent;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>The test stand-in for the Redis store; <see cref="RedisWebSessionStoreTests"/> covers the real one.</summary>
public sealed class InMemoryWebSessionStore : IWebSessionStore
{
    private readonly ConcurrentDictionary<string, WebSession> _sessions = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, WebSession> Sessions => _sessions;

    public Task<WebSession?> GetAsync(string webSessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(webSessionId, out var session) ? session : null);

    public Task SaveAsync(string webSessionId, WebSession session, CancellationToken cancellationToken = default)
    {
        _sessions[webSessionId] = session;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string webSessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(webSessionId, out _);
        return Task.CompletedTask;
    }
}
