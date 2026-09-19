using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Time.Testing;
using Simulab.Testing;
using Simulab.Web.Services.Auth;
using StackExchange.Redis;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// B-3, BR1: the real store, against the shared Redis container. The test connects with a plain client:
/// the container has no TLS, and the Aspire client rule is about the app's own wiring, which the app host
/// check exercises.
/// </summary>
public sealed class RedisWebSessionStoreTests : IAsyncLifetime
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero));
    private readonly IDataProtectionProvider _dataProtection = new EphemeralDataProtectionProvider();
    private ConnectionMultiplexer? _redis;

    public async Task InitializeAsync() =>
        _redis = await ConnectionMultiplexer.ConnectAsync(await RedisServer.ConnectionStringAsync());

    private RedisWebSessionStore CreateStore() => new(_redis!, _dataProtection, _clock);

    private WebSession Session(TimeSpan expiresIn) =>
        new("jti-1", "access-1", "refresh-1", _clock.GetUtcNow().AddMinutes(15), ["identity.roles.manage"], _clock.GetUtcNow(), _clock.GetUtcNow().Add(expiresIn));

    [Fact]
    public async Task Save_ThenGet_RoundTripsAndExpiresWithTheCookie()
    {
        var store = CreateStore();
        var id = Guid.NewGuid().ToString("N");
        var session = Session(TimeSpan.FromDays(12));

        await store.SaveAsync(id, session);
        var read = await store.GetAsync(id);
        var ttl = await _redis!.GetDatabase().KeyTimeToLiveAsync($"web:session:{id}");

        read.Should().BeEquivalentTo(session);
        ttl.Should().BeCloseTo(TimeSpan.FromDays(12), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Save_StoresNoTokenInClearText()
    {
        var id = Guid.NewGuid().ToString("N");

        await CreateStore().SaveAsync(id, Session(TimeSpan.FromDays(30)));
        var raw = (await _redis!.GetDatabase().StringGetAsync($"web:session:{id}")).ToString();

        raw.Should().NotContain("access-1").And.NotContain("refresh-1");
    }

    [Fact]
    public async Task Get_EntryProtectedWithOtherKeys_ReturnsNull()
    {
        var id = Guid.NewGuid().ToString("N");
        await new RedisWebSessionStore(_redis!, new EphemeralDataProtectionProvider(), _clock).SaveAsync(id, Session(TimeSpan.FromDays(30)));

        (await CreateStore().GetAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Remove_ThenGet_ReturnsNull()
    {
        var store = CreateStore();
        var id = Guid.NewGuid().ToString("N");
        await store.SaveAsync(id, Session(TimeSpan.FromDays(30)));

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
