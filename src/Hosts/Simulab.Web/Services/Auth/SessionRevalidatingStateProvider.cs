using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// B-3, BR5: with a page open there is no HTTP request to check the cookie on, so the circuit asks the
/// Api every <see cref="WebSessionTokenAccessor.CheckInterval"/>. An ended session turns the state
/// anonymous; <c>SessionEndedWatcher</c> then sends the browser to sign in again.
/// </summary>
public sealed class SessionRevalidatingStateProvider(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => WebSessionTokenAccessor.CheckInterval;

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);

        var webSessionId = authenticationState.User.FindFirstValue(WebAuthClaims.WebSessionId);
        if (webSessionId is null)
        {
            return authenticationState.User.Identity?.IsAuthenticated != true;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<WebSessionTokenAccessor>();
        return await accessor.CheckAsync(webSessionId, force: true, cancellationToken) is not null;
    }
}
