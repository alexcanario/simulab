using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>B-3, BR5: with a page open, the circuit confirms the session with the Api on each revalidation.</summary>
public sealed class SessionRevalidationTests : IAsyncDisposable
{
    private const string WebSessionId = "web-1";

    private readonly FakeAuthApi _api = new();
    private readonly InMemoryWebSessionStore _store = new();
    private readonly ServiceProvider _services;

    public SessionRevalidationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebSessionStore>(_store);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<SessionRefreshGate>();
        services.AddScoped(_ => new AuthClient(_api.Client(), Options.Create(new OpenIddictClientOptions())));
        services.AddScoped<WebSessionTokenAccessor>();
        _services = services.BuildServiceProvider();

        _store.SaveAsync(WebSessionId, new WebSession("jti-0", "access-0", "refresh-0", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
    }

    [Fact]
    public async Task Revalidate_RevokedSession_ReturnsFalse()
    {
        _api.SessionStatus = HttpStatusCode.Unauthorized;

        var valid = await RevalidateAsync(SignedIn(WebSessionId));

        valid.Should().BeFalse();
        _store.Sessions.Should().NotContainKey(WebSessionId);
    }

    [Fact]
    public async Task Revalidate_LiveSession_ReturnsTrueAndAsksTheApiEveryTime()
    {
        var first = await RevalidateAsync(SignedIn(WebSessionId));
        var second = await RevalidateAsync(SignedIn(WebSessionId));

        (first && second).Should().BeTrue();
        _api.SessionCount.Should().Be(2);
    }

    [Fact]
    public async Task Revalidate_Anonymous_ReturnsTrue()
    {
        var valid = await RevalidateAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        valid.Should().BeTrue();
        _api.SessionCount.Should().Be(0);
    }

    private static ClaimsPrincipal SignedIn(string webSessionId) =>
        new(new ClaimsIdentity([new Claim(WebAuthClaims.WebSessionId, webSessionId)], "Cookies"));

    /// <summary>The framework calls this protected method on its own timer; the test calls it directly.</summary>
    private async Task<bool> RevalidateAsync(ClaimsPrincipal user)
    {
        using var provider = new SessionRevalidatingStateProvider(NullLoggerFactory.Instance, _services.GetRequiredService<IServiceScopeFactory>());
        var method = typeof(SessionRevalidatingStateProvider).GetMethod("ValidateAuthenticationStateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<bool>)method.Invoke(provider, [new AuthenticationState(user), CancellationToken.None])!;
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        _api.Dispose();
    }
}
