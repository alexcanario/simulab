using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR2 and BR3: the one place Web code gets an access token from.</summary>
public sealed class WebSessionTokenAccessorTests : IDisposable
{
    private const string WebSessionId = "web-1";

    private readonly FakeAuthApi _api = new();
    private readonly InMemoryWebSessionStore _store = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero));

    private WebSessionTokenAccessor CreateAccessor(SessionRefreshGate? gate = null) =>
        new(_store, new AuthClient(_api.Client(), Options.Create(new OpenIddictClientOptions { ClientId = "simulab-web", ClientSecret = "secret" })), gate ?? new SessionRefreshGate(), _clock);

    private Task StoreSession(TimeSpan expiresIn, IReadOnlyList<string>? permissions = null) =>
        _store.SaveAsync(WebSessionId, new WebSession("jti-0", "access-0", "refresh-0", _clock.GetUtcNow().Add(expiresIn), permissions ?? [], _clock.GetUtcNow()));

    private static ClaimsPrincipal UserWith(string webSessionId) =>
        new(new ClaimsIdentity([new Claim(WebAuthClaims.WebSessionId, webSessionId)], "test"));

    [Fact]
    public async Task GetAccessToken_ValidAccessToken_ReturnsItWithoutRefreshing()
    {
        await StoreSession(TimeSpan.FromMinutes(10));

        var token = await CreateAccessor().GetAccessTokenAsync(UserWith(WebSessionId));

        token.Should().Be("access-0");
        _api.RefreshCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAccessToken_ExpiredAccessToken_RefreshesAndStoresNewPairAndPermissions()
    {
        await StoreSession(TimeSpan.FromSeconds(30), permissions: ["old.permission"]);
        _api.Permissions = ["identity.roles.manage"];

        var token = await CreateAccessor().GetAccessTokenAsync(UserWith(WebSessionId));

        token.Should().Be("access-1");
        var stored = _store.Sessions[WebSessionId];
        stored.RefreshToken.Should().Be("refresh-1");
        stored.ApiSessionJti.Should().Be("jti-of-access-1");
        stored.AccessTokenExpiresAt.Should().Be(_clock.GetUtcNow().AddSeconds(900));
        stored.Permissions.Should().Equal("identity.roles.manage");
    }

    [Fact]
    public async Task GetAccessToken_ConcurrentCallsOnExpiredToken_RefreshesOnce()
    {
        await StoreSession(TimeSpan.Zero);
        _api.RefreshGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new SessionRefreshGate();

        var first = CreateAccessor(gate).GetAccessTokenAsync(UserWith(WebSessionId));
        var second = CreateAccessor(gate).GetAccessTokenAsync(UserWith(WebSessionId));
        _api.RefreshGate.SetResult();

        var tokens = await Task.WhenAll(first, second);

        tokens.Should().Equal("access-1", "access-1");
        _api.RefreshCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAccessToken_RefreshRejected_EndsSessionAndRemovesEntry()
    {
        await StoreSession(TimeSpan.Zero);
        _api.Refresh = RefreshAnswer.Rejected;

        var token = await CreateAccessor().GetAccessTokenAsync(UserWith(WebSessionId));

        token.Should().BeNull();
        _store.Sessions.Should().NotContainKey(WebSessionId);
    }

    [Fact]
    public async Task GetAccessToken_ApiUnreachable_KeepsSession()
    {
        await StoreSession(TimeSpan.Zero);
        _api.Refresh = RefreshAnswer.Unreachable;

        var token = await CreateAccessor().GetAccessTokenAsync(UserWith(WebSessionId));

        token.Should().Be("access-0");
        _store.Sessions.Should().ContainKey(WebSessionId);
    }

    [Fact]
    public async Task GetAccessToken_NoStoredSession_ReturnsNull()
    {
        var token = await CreateAccessor().GetAccessTokenAsync(UserWith("unknown"));

        token.Should().BeNull();
        _api.RefreshCount.Should().Be(0);
    }

    [Fact]
    public async Task Check_RecentlyCheckedWithoutForce_DoesNotCallApi()
    {
        await StoreSession(TimeSpan.FromMinutes(10));
        _clock.Advance(TimeSpan.FromSeconds(30));

        var session = await CreateAccessor().CheckAsync(WebSessionId, force: false);

        session.Should().NotBeNull();
        _api.SessionCount.Should().Be(0);
    }

    [Fact]
    public async Task Check_Forced_AsksApiAndStoresPermissions()
    {
        await StoreSession(TimeSpan.FromMinutes(10));
        _api.Permissions = ["identity.roles.manage"];

        var session = await CreateAccessor().CheckAsync(WebSessionId, force: true);

        session!.Permissions.Should().Equal("identity.roles.manage");
        _store.Sessions[WebSessionId].Permissions.Should().Equal("identity.roles.manage");
        _api.SessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Check_ApiSaysEnded_RemovesEntry()
    {
        await StoreSession(TimeSpan.FromMinutes(10));
        _api.SessionStatus = System.Net.HttpStatusCode.Unauthorized;

        var session = await CreateAccessor().CheckAsync(WebSessionId, force: true);

        session.Should().BeNull();
        _store.Sessions.Should().NotContainKey(WebSessionId);
    }

    [Fact]
    public async Task Check_ApiFails_KeepsSession()
    {
        await StoreSession(TimeSpan.FromMinutes(10));
        _api.SessionStatus = System.Net.HttpStatusCode.ServiceUnavailable;

        var session = await CreateAccessor().CheckAsync(WebSessionId, force: true);

        session.Should().NotBeNull();
        _store.Sessions.Should().ContainKey(WebSessionId);
    }

    public void Dispose() => _api.Dispose();
}
