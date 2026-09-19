using Simulab.Identity.Contracts;
using Simulab.Testing;
using Simulab.Web.Services.Auth;
using StackExchange.Redis;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR1: the real store, against the shared Redis container.</summary>
public sealed class RedisWebSessionStoreTests : IAsyncLifetime
{
    private ConnectionMultiplexer? _redis;

    public async Task InitializeAsync() =>
        _redis = await ConnectionMultiplexer.ConnectAsync(await RedisServer.ConnectionStringAsync());

    [Fact]
    public async Task Save_ThenGet_RoundTripsAndExpiresWithRefreshLifetime()
    {
        var store = new RedisWebSessionStore(_redis!);
        var id = Guid.NewGuid().ToString("N");
        var session = new WebSession("jti-1", "access-1", "refresh-1", new DateTimeOffset(2026, 9, 19, 12, 15, 0, TimeSpan.Zero), ["identity.roles.manage"], new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero));

        await store.SaveAsync(id, session);
        var read = await store.GetAsync(id);
        var ttl = await _redis!.GetDatabase().KeyTimeToLiveAsync($"web:session:{id}");

        read.Should().BeEquivalentTo(session);
        ttl.Should().BeCloseTo(TokenLifetimes.RefreshToken, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Remove_ThenGet_ReturnsNull()
    {
        var store = new RedisWebSessionStore(_redis!);
        var id = Guid.NewGuid().ToString("N");
        await store.SaveAsync(id, new WebSession("jti-1", "a", "r", DateTimeOffset.UnixEpoch, [], DateTimeOffset.UnixEpoch));

        await store.RemoveAsync(id);

        (await store.GetAsync(id)).Should().BeNull();
    }

    public async Task DisposeAsync()
    {
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }
    }
}
