using System.Globalization;
using System.Security.Cryptography;
using Simulab.Identity.Application.Passwords;
using Simulab.Identity.Domain.Entities;
using StackExchange.Redis;

namespace Simulab.Identity.Infrastructure.Passwords;

/// <summary>
/// Redis-backed <see cref="IPasswordChangeChallengeStore"/> (F-53 BR6). Its own key prefix keeps it apart from the
/// two-factor challenge: a code-step challenge presented here is simply unknown. The value carries its own expiry,
/// read against the host's <see cref="TimeProvider"/>; the key's time to live only cleans up.
/// </summary>
public sealed class RedisPasswordChangeChallengeStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : IPasswordChangeChallengeStore
{
    private const string Prefix = "identity:password-change-challenge:";
    private const int ChallengeBytes = 32;

    private IDatabase Database => redis.GetDatabase();

    public async Task<string> CreateAsync(
        Guid userId,
        string? securityStamp,
        AccountEventMethod method,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var challenge = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(ChallengeBytes));
        var ttl = expiresAt - timeProvider.GetUtcNow();

        await Database.StringSetAsync(
            Prefix + challenge,
            $"{userId:D}|{securityStamp}|{method}|{expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}",
            ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1));

        return challenge;
    }

    public async Task<PasswordChangeChallenge?> PeekAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (!IsWellFormed(challenge))
        {
            return null;
        }

        return Parse(await Database.StringGetAsync(Prefix + challenge));
    }

    public async Task<bool> TrySpendAsync(string challenge, CancellationToken cancellationToken = default)
    {
        if (!IsWellFormed(challenge))
        {
            return false;
        }

        // GETDEL is atomic: of several concurrent spends exactly one receives the value.
        return Parse(await Database.StringGetDeleteAsync(Prefix + challenge)) is not null;
    }

    private static bool IsWellFormed(string challenge) =>
        !string.IsNullOrWhiteSpace(challenge) && challenge.Length == ChallengeBytes * 2;

    private PasswordChangeChallenge? Parse(RedisValue value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var parts = value.ToString().Split('|', 4);
        return parts.Length == 4
            && Guid.TryParse(parts[0], out var userId)
            && Enum.TryParse<AccountEventMethod>(parts[2], out var method)
            && long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiresAt)
            && timeProvider.GetUtcNow() < DateTimeOffset.FromUnixTimeSeconds(expiresAt)
                ? new PasswordChangeChallenge(userId, parts[1].Length == 0 ? null : parts[1], method)
                : null;
    }
}
