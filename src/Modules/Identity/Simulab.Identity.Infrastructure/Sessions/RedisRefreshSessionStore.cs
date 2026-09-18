using Simulab.Identity.Application.Sessions;
using StackExchange.Redis;

namespace Simulab.Identity.Infrastructure.Sessions;

/// <summary>Redis-backed <see cref="IRefreshSessionStore"/> (BR4-BR7). One key per session, one per revoked access token.</summary>
public sealed class RedisRefreshSessionStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : IRefreshSessionStore
{
    private const string SessionPrefix = "identity:session:";
    private const string RevokedPrefix = "identity:revoked:";

    private IDatabase Database => redis.GetDatabase();

    public Task CreateAsync(string sessionJti, Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(SessionKey(sessionJti), userId.ToString(), TtlUntil(expiresAt));

    public async Task<Guid?> ConsumeAsync(string sessionJti, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetDeleteAsync(SessionKey(sessionJti));
        return value.HasValue && Guid.TryParse(value.ToString(), out var userId) ? userId : null;
    }

    public Task RemoveAsync(string sessionJti, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(SessionKey(sessionJti));

    public Task RevokeAccessTokenAsync(string sessionJti, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(RevokedKey(sessionJti), true, timeToLive > TimeSpan.Zero ? timeToLive : TimeSpan.FromSeconds(1));

    public Task<bool> IsAccessTokenRevokedAsync(string sessionJti, CancellationToken cancellationToken = default) =>
        Database.KeyExistsAsync(RevokedKey(sessionJti));

    private TimeSpan TtlUntil(DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - timeProvider.GetUtcNow();
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }

    private static string SessionKey(string jti) => SessionPrefix + jti;

    private static string RevokedKey(string jti) => RevokedPrefix + jti;
}
