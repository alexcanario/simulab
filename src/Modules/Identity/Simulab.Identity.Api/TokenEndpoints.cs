using System.Security.Claims;
using Microsoft.AspNetCore; // OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest lives here (not in OpenIddict.*).
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Simulab.Identity.Api;

/// <summary>
/// The OpenIddict token endpoint (BR1): password grant for sign-in, refresh grant for a silent renewal.
/// A declared exception to the api-contracts rule (F-5, decision 1): this is OpenIddict's own protocol
/// path, not a REST resource, so it stays outside <c>/api/v1</c> and answers with the OAuth2 error shape
/// (<c>error</c> / <c>error_description</c>), not RFC 9457 problem details.
/// </summary>
public static class TokenEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/connect/token", HandleTokenAsync).WithName("Token");

        return endpoints;
    }

    private static async Task<IResult> HandleTokenAsync(
        HttpContext context,
        UserManager<User> userManager,
        IRefreshSessionStore sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(request, userManager, sessions, timeProvider, cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshGrantAsync(context, userManager, sessions, timeProvider, cancellationToken);
        }

        return Forbid(Errors.UnsupportedGrantType);
    }

    /// <summary>BR2, BR3: lockout is checked before the password, so a correct password during lockout is still refused.</summary>
    private static async Task<IResult> HandlePasswordGrantAsync(
        OpenIddictRequest request,
        UserManager<User> userManager,
        IRefreshSessionStore sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var user = string.IsNullOrWhiteSpace(request.Username) ? null : await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            return Forbid(IdentityErrorCodes.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            var remaining = lockoutEnd.HasValue ? lockoutEnd.Value - timeProvider.GetUtcNow() : TimeSpan.Zero;
            return Forbid(IdentityErrorCodes.AccountLocked, SecondsOf(remaining));
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
        {
            await userManager.AccessFailedAsync(user);
            return Forbid(IdentityErrorCodes.InvalidCredentials);
        }

        if (user.Status != AccountStatus.Active)
        {
            return Forbid(IdentityErrorCodes.EmailNotVerified);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, sessions, timeProvider, cancellationToken);
    }

    /// <summary>BR5: the presented refresh token is consumed exactly once; rotation issues a brand new pair.</summary>
    private static async Task<IResult> HandleRefreshGrantAsync(
        HttpContext context,
        UserManager<User> userManager,
        IRefreshSessionStore sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var sessionJti = result.Principal?.GetClaim(SessionClaims.SessionJti);
        if (result.Principal is null || sessionJti is null)
        {
            return Forbid(IdentityErrorCodes.RefreshTokenInvalid);
        }

        var session = await sessions.ConsumeAsync(sessionJti, cancellationToken);
        if (session is null)
        {
            return Forbid(IdentityErrorCodes.RefreshTokenInvalid);
        }

        // The old access token carries the old session id, which leaves the per-user index with this
        // refresh: revoke it now, or a revoke-all could no longer find it (F-7 BR9).
        await sessions.RevokeAccessTokenAsync(sessionJti, TokenLifetimes.AccessToken, cancellationToken);

        // F-7 BR9: a password reset or change renewed the stamp, so a session from before it ends here,
        // even one a concurrent revoke-all did not see.
        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null || session.SecurityStamp is null || !string.Equals(session.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            return Forbid(IdentityErrorCodes.RefreshTokenInvalid);
        }

        return await IssueTokensAsync(user, sessions, timeProvider, cancellationToken);
    }

    private static async Task<IResult> IssueTokensAsync(
        User user,
        IRefreshSessionStore sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var sessionJti = Guid.CreateVersion7().ToString();
        await sessions.CreateAsync(sessionJti, user.Id, user.SecurityStamp, timeProvider.GetUtcNow().Add(TokenLifetimes.RefreshToken), cancellationToken);

        var identity = new ClaimsIdentity(authenticationType: "OpenIddict", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Email, user.Email)
            .SetClaim(SessionClaims.TenantId, user.TenantId?.ToString() ?? string.Empty)
            .SetClaim(SessionClaims.SessionJti, sessionJti)
            .SetScopes(Scopes.OfflineAccess)
            .SetDestinations(_ => [Destinations.AccessToken]);

        return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static string SecondsOf(TimeSpan remaining) =>
        Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static IResult Forbid(string errorCode, string? errorDescription = null) =>
        Results.Forbid(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorCode,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
}
