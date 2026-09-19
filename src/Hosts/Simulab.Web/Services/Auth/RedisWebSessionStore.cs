using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Simulab.SharedKernel.Serialization;
using StackExchange.Redis;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// Redis-backed <see cref="IWebSessionStore"/> (B-3, BR1): a restart of the Web keeps everyone signed in,
/// which an in-memory store would not (decision, q1). The entry is protected with the same Data
/// Protection keys that protected the tokens when they lived in the cookie, so reading Redis alone
/// does not hand anyone a session.
/// </summary>
public sealed class RedisWebSessionStore(IConnectionMultiplexer redis, IDataProtectionProvider dataProtection, TimeProvider timeProvider)
    : IWebSessionStore
{
    private const string Prefix = "web:session:";

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Simulab.Web.WebSession");

    private IDatabase Database => redis.GetDatabase();

    public async Task<WebSession?> GetAsync(string webSessionId, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(Key(webSessionId));
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WebSession>(_protector.Unprotect(value.ToString()), AppJson.Options);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Written under keys this instance no longer has: the session cannot be vouched for.
            return null;
        }
    }

    public Task SaveAsync(string webSessionId, WebSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var ttl = session.ExpiresAt - timeProvider.GetUtcNow();
        return Database.StringSetAsync(
            Key(webSessionId),
            _protector.Protect(JsonSerializer.Serialize(session, AppJson.Options)),
            ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1));
    }

    public Task RemoveAsync(string webSessionId, CancellationToken cancellationToken = default) =>
        Database.KeyDeleteAsync(Key(webSessionId));

    private static string Key(string webSessionId) => Prefix + webSessionId;
}
