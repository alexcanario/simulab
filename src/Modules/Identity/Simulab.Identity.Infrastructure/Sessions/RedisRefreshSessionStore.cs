using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using StackExchange.Redis;

namespace Simulab.Identity.Infrastructure.Sessions;

/// <summary>
/// Redis-backed <see cref="IRefreshSessionStore"/> (F-5 BR4-BR7). One key per session, one per revoked access
/// token, and one set per user listing the user's sessions (F-7 BR9), so every session of a user can be ended.
/// A stale member in the set (a session that expired on its own) is harmless: ending it finds nothing.
/// </summary>
public sealed class RedisRefreshSessionStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : IRefreshSessionStore
{
    private const string SessionPrefix = "identity:session:";
    private const string RevokedPrefix = "identity:revoked:";
    private const string UserSessionsPrefix = "identity:user-sessions:";

    private IDatabase Database => redis.GetDatabase();

    public async Task CreateAsync(string sessionJti, Guid userId, string? securityStamp, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var ttl = TtlUntil(expiresAt);
        await Database.StringSetAsync(SessionKey(sessionJti), $"{userId:D}|{securityStamp}", ttl);

        // The index lives as long as the newest session in it.
        await Database.SetAddAsync(UserSessionsKey(userId), sessionJti);
        await Database.KeyExpireAsync(UserSessionsKey(userId), ttl);
    }

    public async Task<RefreshSession?> ConsumeAsync(string sessionJti, CancellationToken cancellationToken = default)
    {
        var session = Parse(await Database.StringGetDeleteAsync(SessionKey(sessionJti)));
        if (session is null)
        {
            return null;
        }

        await Database.SetRemoveAsync(UserSessionsKey(session.UserId), sessionJti);
        return session;
    }

    public async Task RemoveAsync(string sessionJti, CancellationToken cancellationToken = default)
    {
        var session = Parse(await Database.StringGetDeleteAsync(SessionKey(sessionJti)));
        if (session is not null)
        {
            await Database.SetRemoveAsync(UserSessionsKey(session.UserId), sessionJti);
        }
    }

    public async Task RestampAsync(string sessionJti, string? securityStamp, CancellationToken cancellationToken = default)
    {
        var session = Parse(await Database.StringGetAsync(SessionKey(sessionJti)));
        if (session is not null)
        {
            await Database.StringSetAsync(SessionKey(sessionJti), $"{session.UserId:D}|{securityStamp}", expiry: null, keepTtl: true, when: When.Exists);
        }
    }

    public async Task RevokeAllAsync(Guid userId, string? exceptSessionJti = null, CancellationToken cancellationToken = default)
    {
        foreach (var member in await Database.SetMembersAsync(UserSessionsKey(userId)))
        {
            var sessionJti = member.ToString();
            if (sessionJti == exceptSessionJti)
            {
                continue;
            }

            await Database.KeyDeleteAsync(SessionKey(sessionJti));
            await RevokeAccessTokenAsync(sessionJti, TokenLifetimes.AccessToken, cancellationToken);
            await Database.SetRemoveAsync(UserSessionsKey(userId), sessionJti);
        }
    }

    public Task RevokeAccessTokenAsync(string sessionJti, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(RevokedKey(sessionJti), true, timeToLive > TimeSpan.Zero ? timeToLive : TimeSpan.FromSeconds(1));

    public Task<bool> IsAccessTokenRevokedAsync(string sessionJti, CancellationToken cancellationToken = default) =>
        Database.KeyExistsAsync(RevokedKey(sessionJti));

    private TimeSpan TtlUntil(DateTimeOffset expiresAt)
    {
        var ttl = expiresAt - timeProvider.GetUtcNow();
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }

    /// <summary>The value is <c>userId|securityStamp</c>. A value from before F-7 has no stamp and reads as a stale session.</summary>
    private static RefreshSession? Parse(RedisValue value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var parts = value.ToString().Split('|', 2);
        return Guid.TryParse(parts[0], out var userId)
            ? new RefreshSession(userId, parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null)
            : null;
    }

    private static string SessionKey(string jti) => SessionPrefix + jti;

    private static string RevokedKey(string jti) => RevokedPrefix + jti;

    private static string UserSessionsKey(Guid userId) => UserSessionsPrefix + userId.ToString("N");
}
