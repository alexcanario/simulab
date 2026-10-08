using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Simulab.Identity.Api;

/// <summary>
/// The per-client limits of BR12: a fixed window per key. It counts by client address, so it never
/// depends on whether an account exists and cannot be used to find one (BR4). F-38 adds the sign-in limit,
/// which counts the distinct account names that failed from one address instead of the calls.
/// </summary>
/// <remarks>
/// F-54: the counters live in Redis, so every Api replica counts into the same window. Each operation is one
/// Lua script (atomic on the server) that gets the time from the host's <see cref="TimeProvider"/>; the
/// window's start and length live in the value and the key's time to live only cleans up, as in
/// <c>RedisTotpChallengeStore</c>. Every window keeps its own length, so a short window never ends a long one
/// (F-38 AC13). When Redis cannot be reached the call is allowed and not counted (BR5): the limits are an
/// abuse brake, and registration, resend and reset must stay up.
/// </remarks>
public sealed class ClientRateLimiter
{
    private const string KeyPrefix = "identity:rate-limit:";
    private const string NameKeyLabel = "simulab/identity/rate-limit-names";
    private static readonly TimeSpan OutageLogEvery = TimeSpan.FromMinutes(1);

    // KEYS[1]; ARGV: now (ms), limit, window (ms). Returns 1 when the call is allowed and counted.
    private const string AcquireScript = """
        local now = tonumber(ARGV[1])
        local s = tonumber(redis.call('HGET', KEYS[1], 's'))
        local l = tonumber(redis.call('HGET', KEYS[1], 'l'))
        if s == nil or now - s >= l then
          redis.call('DEL', KEYS[1])
          redis.call('HSET', KEYS[1], 's', now, 'l', ARGV[3], 'c', 1)
          redis.call('PEXPIRE', KEYS[1], ARGV[3])
          return 1
        end
        if tonumber(redis.call('HGET', KEYS[1], 'c')) >= tonumber(ARGV[2]) then
          return 0
        end
        redis.call('HINCRBY', KEYS[1], 'c', 1)
        return 1
        """;

    // KEYS[1]; ARGV: now (ms), limit, window (ms), name hash. Returns {allowed, retry after (ms), first refusal}.
    private const string ReserveScript = """
        local now = tonumber(ARGV[1])
        local s = tonumber(redis.call('HGET', KEYS[1], 's'))
        local l = tonumber(redis.call('HGET', KEYS[1], 'l'))
        if s == nil or now - s >= l then
          redis.call('DEL', KEYS[1])
          redis.call('HSET', KEYS[1], 's', now, 'l', ARGV[3], 'c', 0, 'r', 0)
          redis.call('PEXPIRE', KEYS[1], ARGV[3])
          s = now
          l = tonumber(ARGV[3])
        end
        if tonumber(redis.call('HGET', KEYS[1], 'c')) >= tonumber(ARGV[2]) then
          local first = 0
          if redis.call('HGET', KEYS[1], 'r') == '0' then
            redis.call('HSET', KEYS[1], 'r', 1)
            first = 1
          end
          return {0, s + l - now, first}
        end
        if redis.call('HSETNX', KEYS[1], 'n:' .. ARGV[4], 1) == 1 then
          redis.call('HINCRBY', KEYS[1], 'c', 1)
        end
        return {1, 0, 0}
        """;

    // KEYS[1]; ARGV: name hash.
    private const string ReleaseScript = """
        if redis.call('HDEL', KEYS[1], 'n:' .. ARGV[1]) == 1 then
          redis.call('HINCRBY', KEYS[1], 'c', -1)
        end
        return 1
        """;

    // KEYS[1]; ARGV: now (ms), limit. Reads only; the first refusal of the window still says so.
    private const string CheckScript = """
        local now = tonumber(ARGV[1])
        local s = tonumber(redis.call('HGET', KEYS[1], 's'))
        local l = tonumber(redis.call('HGET', KEYS[1], 'l'))
        if s == nil or now - s >= l then
          return {1, 0, 0}
        end
        if tonumber(redis.call('HGET', KEYS[1], 'c')) < tonumber(ARGV[2]) then
          return {1, 0, 0}
        end
        local first = 0
        if redis.call('HGET', KEYS[1], 'r') == '0' then
          redis.call('HSET', KEYS[1], 'r', 1)
          first = 1
        end
        return {0, s + l - now, first}
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClientRateLimiter> _logger;

    // Empty in every deployed environment. A test host sets 'Identity:RateLimit:Namespace' so that hosts sharing one
    // Redis container (and all calling from no address) do not count into each other's windows.
    private readonly string _keyPrefix;

    // F-38 BR2, F-54 BR4: the names are kept as keyed hashes, never as typed. The key comes from a secret the Api
    // already holds, so every replica hashes a name the same way, and it is never written to Redis.
    private readonly byte[] _nameKey;
    private long _lastOutageLogTicks;

    public ClientRateLimiter(
        IConnectionMultiplexer redis,
        TimeProvider timeProvider,
        IConfiguration configuration,
        ILogger<ClientRateLimiter> logger)
    {
        _redis = redis;
        _timeProvider = timeProvider;
        _logger = logger;

        var keyspace = configuration["Identity:RateLimit:Namespace"];
        _keyPrefix = string.IsNullOrEmpty(keyspace) ? KeyPrefix : $"{KeyPrefix}{keyspace}:";

        var secret = configuration["Authentication:OpenIddict:ClientSecret"]
            ?? throw new InvalidOperationException("The configuration 'Authentication:OpenIddict:ClientSecret' is missing.");
        _nameKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            Encoding.UTF8.GetBytes(secret),
            outputLength: 32,
            salt: null,
            info: Encoding.UTF8.GetBytes(NameKeyLabel));
    }

    /// <summary>True when the call is allowed and counted; false when the window is full.</summary>
    public async Task<bool> TryAcquireAsync(string key, int permitLimit, TimeSpan window)
    {
        try
        {
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                AcquireScript,
                [_keyPrefix + key],
                [NowMilliseconds(), permitLimit, (long)window.TotalMilliseconds]);
            return (long)result == 1;
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogOutage(exception);
            return true;
        }
    }

    /// <summary>
    /// F-38 BR1, BR3: adds the account name to the address's set, atomically. Refused (nothing added) while the
    /// set already holds <paramref name="nameLimit"/> names, whatever the name; so parallel calls cannot pass a
    /// check together (AC14), on any replica.
    /// </summary>
    public async Task<SignInReservation> ReserveSignInNameAsync(string key, string accountName, int nameLimit, TimeSpan window)
    {
        try
        {
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                ReserveScript,
                [_keyPrefix + key],
                [NowMilliseconds(), nameLimit, (long)window.TotalMilliseconds, Hash(accountName)]);
            return ToReservation(result);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogOutage(exception);
            return Allowed;
        }
    }

    /// <summary>F-38 BR5: takes only this account's name out of the address's set, if it is there.</summary>
    public async Task ReleaseSignInNameAsync(string key, string accountName)
    {
        try
        {
            await _redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, [_keyPrefix + key], [Hash(accountName)]);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogOutage(exception);
        }
    }

    /// <summary>
    /// F-38 BR3: whether the address may sign in now, without adding a name. Refused while its set is full, with
    /// the time left; the code step reads it before it touches the challenge.
    /// </summary>
    public async Task<SignInReservation> CheckSignInAsync(string key, int nameLimit)
    {
        try
        {
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                CheckScript,
                [_keyPrefix + key],
                [NowMilliseconds(), nameLimit]);
            return ToReservation(result);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogOutage(exception);
            return Allowed;
        }
    }

    private static SignInReservation Allowed => new(true, TimeSpan.Zero, false);

    private static SignInReservation ToReservation(RedisResult result)
    {
        var parts = (long[])result!;
        return parts[0] == 1
            ? Allowed
            : new SignInReservation(false, TimeSpan.FromMilliseconds(parts[1]), parts[2] == 1);
    }

    private static bool IsRedisFailure(Exception exception) => exception is RedisConnectionException or RedisTimeoutException or TimeoutException;

    private long NowMilliseconds() => _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    private string Hash(string accountName) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_nameKey, Encoding.UTF8.GetBytes(accountName)));

    /// <summary>BR5: one error line per replica at most once a minute while the outage lasts.</summary>
    private void LogOutage(Exception exception)
    {
        var now = _timeProvider.GetUtcNow().UtcTicks;
        var last = Interlocked.Read(ref _lastOutageLogTicks);
        if (last != 0
            && now - last < OutageLogEvery.Ticks
            || Interlocked.CompareExchange(ref _lastOutageLogTicks, now, last) != last)
        {
            return;
        }

        _logger.LogError(exception, "The client rate limits cannot reach Redis; calls are allowed and not counted until it is back");
    }
}

/// <summary>
/// What <see cref="ClientRateLimiter.ReserveSignInNameAsync"/> decided: whether the name was added, the time
/// left in the window when it was not, and whether this refusal is the first of the window (BR7).
/// </summary>
public readonly record struct SignInReservation(bool Allowed, TimeSpan RetryAfter, bool FirstRefusal);
