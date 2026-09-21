using System.Globalization;
using System.Security.Cryptography;
using Simulab.Identity.Application.Totp;
using StackExchange.Redis;

namespace Simulab.Identity.Infrastructure.Totp;

/// <summary>
/// Redis-backed <see cref="ITotpChallengeStore"/> (F-11 BR9). The value carries its own expiry, read against the
/// host's <see cref="TimeProvider"/>; the key's time to live only cleans up.
/// </summary>
public sealed class RedisTotpChallengeStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : ITotpChallengeStore
{
    private const string Prefix = "identity:totp-challenge:";
    private const int ChallengeBytes = 32;

    private IDatabase Database => redis.GetDatabase();

    public async Task<string> CreateAsync(Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var challenge = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(ChallengeBytes));
        var ttl = expiresAt - timeProvider.GetUtcNow();

        await Database.StringSetAsync(
            Prefix + challenge,
            $"{userId:D}|{expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}",
            ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1));

        return challenge;
    }

    public async Task<Guid?> ConsumeAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challenge) || challenge.Length != ChallengeBytes * 2)
        {
            return null;
        }

        var value = await Database.StringGetDeleteAsync(Prefix + challenge);
        if (!value.HasValue)
        {
            return null;
        }

        var parts = value.ToString().Split('|', 2);
        return parts.Length == 2
            && Guid.TryParse(parts[0], out var userId)
            && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiresAt)
            && timeProvider.GetUtcNow() < DateTimeOffset.FromUnixTimeSeconds(expiresAt)
                ? userId
                : null;
    }
}
