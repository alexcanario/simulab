using System.Security.Claims;
using Microsoft.AspNetCore; // OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest lives here (not in OpenIddict.*).
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.GoogleSignIn;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure;
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
        IAccountEventLog accountEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(context, request, userManager, sessions, accountEvents, timeProvider, cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshGrantAsync(context, userManager, sessions, timeProvider, cancellationToken);
        }

        if (request.GrantType == IdentityModule.TotpGrantType && TotpEnabled(context))
        {
            return await HandleTotpGrantAsync(context, request, sessions, timeProvider, cancellationToken);
        }

        if (request.GrantType == GoogleSignInProtocol.GrantType && GoogleSignInEnabled(context))
        {
            return await HandleGoogleGrantAsync(context, request, sessions, accountEvents, timeProvider, cancellationToken);
        }

        return Forbid(Errors.UnsupportedGrantType);
    }

    /// <summary>F-20 BR1: read per request from the options, the same switch that registers the grant.</summary>
    private static bool GoogleSignInEnabled(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<GoogleSignInOptions>>().Value.GoogleSignInEnabled;

    /// <summary>
    /// F-20: the Google step. No account → the confirmation page (BR7). Two-factor on → the same challenge as after a
    /// password, with the failure count untouched (BR4, change note v2). Otherwise Google is the last step: the count
    /// and any lockout are cleared and the tokens issued.
    /// </summary>
    private static async Task<IResult> HandleGoogleGrantAsync(
        HttpContext context,
        OpenIddictRequest request,
        IRefreshSessionStore sessions,
        IAccountEventLog accountEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // Resolved here, not as a parameter: it exists only while the feature is on.
        var handler = context.RequestServices.GetRequiredService<GoogleSignInHandler>();
        var result = await handler.SignInAsync((string?)request[GoogleSignInProtocol.IdTokenParameter], cancellationToken);
        if (result.IsFailure)
        {
            // F-21 BR4: a token Google's own checks refused. The other two answers of this step are not failed
            // sign-ins — one sends the visitor to the confirmation page, the other tells them to use the
            // password — and no account was ever named by them.
            if (result.Error!.Code is IdentityErrorCodes.GoogleTokenInvalid or IdentityErrorCodes.GoogleEmailNotVerified)
            {
                await accountEvents.SignInFailedAsync(null, AccountEventReason.GoogleTokenRefused, cancellationToken);
            }

            return Forbid(result.Error!.Code);
        }

        if (result.Value.SignUpRequired is { } google)
        {
            return Forbid(IdentityErrorCodes.GoogleSignUpRequired, parameters: new Dictionary<string, object?>
            {
                [GoogleSignInProtocol.EmailParameter] = google.Email,
                [GoogleSignInProtocol.NameParameter] = google.Name,
            });
        }

        var user = result.Value.User!;
        if (user.TwoFactorEnabled && TotpEnabled(context))
        {
            var challenge = await context.RequestServices.GetRequiredService<TotpSignInHandler>().IssueChallengeAsync(user, cancellationToken);
            return Forbid(IdentityErrorCodes.TotpRequired, parameters: new Dictionary<string, object?>
            {
                [TotpChallengeParameter] = challenge,
                [Parameters.ExpiresIn] = (long)TotpSignInHandler.ChallengeLifetime.TotalSeconds,
            });
        }

        await handler.ClearFailuresAsync(user);

        // F-21 BR3: Google was the last step.
        await accountEvents.SignInSucceededAsync(user.Id, AccountEventMethod.Google, cancellationToken);
        return await IssueTokensAsync(user, sessions, timeProvider, cancellationToken);
    }

    /// <summary>F-11 BR12: read per request from the options, the same switch that maps the routes.</summary>
    private static bool TotpEnabled(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<TotpOptions>>().Value.TotpEnabled;

    /// <summary>
    /// F-11 BR9: the code step. The challenge is spent whatever the code; a wrong code counts on the lockout
    /// (BR10) and a lockout answers with the seconds left, as the password step does.
    /// </summary>
    private static async Task<IResult> HandleTotpGrantAsync(
        HttpContext context,
        OpenIddictRequest request,
        IRefreshSessionStore sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // Resolved here, not as a parameter: it exists only while the feature is on.
        var handler = context.RequestServices.GetRequiredService<TotpSignInHandler>();

        // F-38 BR3: an address at its limit is refused before the challenge is read.
        var attempt = StartAttempt(context);
        if (await attempt.IsAtLimitAsync())
        {
            return Forbid(IdentityErrorCodes.SignInRateLimited, SecondsOf(attempt.RetryAfter));
        }

        // F-21 BR3, BR4: the handler records this step's event — only it knows which account the spent
        // challenge belonged to, and which of the two codes was accepted.
        var result = await handler.CompleteAsync((string?)request[TotpChallengeParameter], (string?)request[TotpCodeParameter], attempt, cancellationToken);

        if (result.IsSuccess)
        {
            return await IssueTokensAsync(result.Value.User, sessions, timeProvider, cancellationToken);
        }

        return Forbid(
            result.Error!.Code,
            result.Error.Code is IdentityErrorCodes.AccountLocked or IdentityErrorCodes.SignInRateLimited ? result.Error.Detail : null);
    }

    /// <summary>F-38: this request's side of the per-address limit, keyed by the client address (BR6, BR8).</summary>
    private static SignInAttempt StartAttempt(HttpContext context)
    {
        var services = context.RequestServices;
        var addresses = services.GetRequiredService<ClientAddress>();
        return new SignInAttempt(
            services.GetRequiredService<ClientRateLimiter>(),
            addresses.KeyFor(context, "sign-in"),
            addresses.Of(context),
            services.GetRequiredService<ILoggerFactory>().CreateLogger<SignInAttempt>());
    }

    /// <summary>BR2, BR3: lockout is checked before the password, so a correct password during lockout is still refused.</summary>
    private static async Task<IResult> HandlePasswordGrantAsync(
        HttpContext context,
        OpenIddictRequest request,
        UserManager<User> userManager,
        IRefreshSessionStore sessions,
        IAccountEventLog accountEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // F-38 BR1, BR3: the typed name is counted first, so an address at its limit costs no account lookup,
        // no failure count, no account event and no lockout. A step that is not a failure takes the name out again.
        var attempt = StartAttempt(context);
        var typedName = request.Username ?? string.Empty;
        if (!await attempt.TryCountAsync(typedName))
        {
            return Forbid(IdentityErrorCodes.SignInRateLimited, SecondsOf(attempt.RetryAfter));
        }

        var user = string.IsNullOrWhiteSpace(request.Username) ? null : await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            // F-21 BR4: the attempt is recorded, the typed name is not.
            await accountEvents.SignInFailedAsync(null, AccountEventReason.UnknownAccount, cancellationToken);
            return Forbid(IdentityErrorCodes.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await accountEvents.SignInFailedAsync(user.Id, AccountEventReason.LockedOut, cancellationToken);
            var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            var remaining = lockoutEnd.HasValue ? lockoutEnd.Value - timeProvider.GetUtcNow() : TimeSpan.Zero;
            return Forbid(IdentityErrorCodes.AccountLocked, SecondsOf(remaining));
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
        {
            // F-47 BR7, no transaction: one write (the failure count); the events and the Redis session follow it.
            await userManager.AccessFailedAsync(user);
            await accountEvents.SignInFailedAsync(user.Id, AccountEventReason.WrongPassword, cancellationToken);

            // F-21 BR5: this failure is what crossed the limit; the attempts that follow are failures, not lockouts.
            if (await userManager.IsLockedOutAsync(user))
            {
                await accountEvents.AccountLockedAsync(user.Id, cancellationToken);
            }

            return Forbid(IdentityErrorCodes.InvalidCredentials);
        }

        // F-38 BR1, BR5: the password was right, so this account's name leaves the set from here on.
        await attempt.ClearAsync(typedName);

        if (user.Status != AccountStatus.Active)
        {
            await accountEvents.SignInFailedAsync(user.Id, AccountEventReason.EmailNotVerified, cancellationToken);
            return Forbid(IdentityErrorCodes.EmailNotVerified);
        }

        // F-11 BR9, BR12: with the feature on, an account with two-factor gets a challenge instead of tokens.
        // With it off, the flag is ignored, so nobody is locked out by the switch. The failure count is not
        // cleared here: each code needs a new password step, and clearing it would give whoever has the
        // password unlimited codes (BR10). The code step clears it.
        if (user.TwoFactorEnabled && TotpEnabled(context))
        {
            var challenge = await context.RequestServices.GetRequiredService<TotpSignInHandler>().IssueChallengeAsync(user, cancellationToken);
            return Forbid(IdentityErrorCodes.TotpRequired, parameters: new Dictionary<string, object?>
            {
                [TotpChallengeParameter] = challenge,
                [Parameters.ExpiresIn] = (long)TotpSignInHandler.ChallengeLifetime.TotalSeconds,
            });
        }

        await userManager.ResetAccessFailedCountAsync(user);

        // F-21 BR3: the password was the last step, so this is the sign-in.
        await accountEvents.SignInSucceededAsync(user.Id, AccountEventMethod.Password, cancellationToken);
        return await IssueTokensAsync(user, sessions, timeProvider, cancellationToken);
    }

    /// <summary>The token-request and error-response parameter names of the code step (F-11).</summary>
    public const string TotpChallengeParameter = "challenge";
    public const string TotpCodeParameter = "code";

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

    /// <summary><paramref name="parameters"/> are written into the error response next to <c>error</c> (F-11: the challenge).</summary>
    private static IResult Forbid(string errorCode, string? errorDescription = null, IDictionary<string, object?>? parameters = null) =>
        Results.Forbid(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(
                new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorCode,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
                },
                parameters ?? new Dictionary<string, object?>()));
}
