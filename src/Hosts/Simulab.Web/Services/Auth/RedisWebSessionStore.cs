using System.Text.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using StackExchange.Redis;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// Redis-backed <see cref="IWebSessionStore"/> (B-3, BR1): a restart of the Web keeps everyone signed in,
/// which an in-memory store would not (decision, q1).
/// </summary>
public sealed class RedisWebSessionStore(IConnectionMultiplexer redis) : IWebSessionStore
{
    private const string Prefix = "web:session:";

    private IDatabase Database => redis.GetDatabase();

    public async Task<WebSession?> GetAsync(string webSessionId, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(Key(webSessionId));
        return value.HasValue ? JsonSerializer.Deserialize<WebSession>(value.ToString(), AppJson.Options) : null;
    }

    public Task SaveAsync(string webSessionId, WebSession session, CancellationToken cancellationToken = default) =>
        Database.StringSetAsync(Key(webSessionId), JsonSerializer.Serialize(session, AppJson.Options), TokenLifetimes.RefreshToken);

    public Task RemoveAsync(string webSessionId, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(Key(webSessionId));

    private static string Key(string webSessionId) => Prefix + webSessionId;
}
