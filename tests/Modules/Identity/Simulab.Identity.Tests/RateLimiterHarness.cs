using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Simulab.Identity.Api;
using Simulab.Testing;
using StackExchange.Redis;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-54: limiters over the test project's Redis container, each class in a keyspace of its own. Two limiters made
/// by <see cref="NewLimiter"/> stand for two Api replicas: same Redis, same client secret, nothing else shared.
/// </summary>
public abstract class RateLimiterHarness : IAsyncLifetime, IDisposable
{
    protected const string ClientSecret = "rate-limit-test-client-secret";

    protected string Keyspace { get; } = Guid.NewGuid().ToString("N");
    private readonly RecordingLoggerProvider _logs = new();
    private readonly ILoggerFactory _loggers;

    protected RateLimiterHarness() => _loggers = LoggerFactory.Create(logging => logging.AddProvider(_logs));

    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));

    protected IConnectionMultiplexer Redis { get; private set; } = null!;

    protected IReadOnlyList<RecordedLogEntry> Logs => _logs.Entries;

    /// <summary>The keys this class's limiters wrote, as Redis holds them.</summary>
    protected string KeyPattern => $"identity:rate-limit:{Keyspace}:*";

    public async Task InitializeAsync() =>
        Redis = await ConnectionMultiplexer.ConnectAsync(await RedisServer.ConnectionStringAsync());

    public async Task DisposeAsync()
    {
        foreach (var key in Keys())
        {
            await Redis.GetDatabase().KeyDeleteAsync(key);
        }

        await Redis.DisposeAsync();
    }

    public void Dispose()
    {
        _loggers.Dispose();
        _logs.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>One more replica: its own limiter object over <paramref name="redis"/> (the shared one by default).</summary>
    protected ClientRateLimiter NewLimiter(string secret = ClientSecret, IConnectionMultiplexer? redis = null) =>
        new(
            redis ?? Redis,
            Clock,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:OpenIddict:ClientSecret"] = secret,
                    ["Identity:RateLimit:Namespace"] = Keyspace,
                })
                .Build(),
            _loggers.CreateLogger<ClientRateLimiter>());

    /// <summary>A client whose Redis nothing listens to: calls fail at once instead of waiting for a timeout.</summary>
    protected static async Task<IConnectionMultiplexer> UnreachableRedisAsync() =>
        await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
        {
            EndPoints = { "localhost:1" },
            AbortOnConnectFail = false,
            ConnectTimeout = 200,
            AsyncTimeout = 500,
            BacklogPolicy = BacklogPolicy.FailFast,
        });

    protected IEnumerable<RedisKey> Keys() => Redis.GetServers().SelectMany(server => server.Keys(pattern: KeyPattern));
}
