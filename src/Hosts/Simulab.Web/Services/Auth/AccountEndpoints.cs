using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Simulab.Identity.Contracts;
using Simulab.Web.Localization;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// Plain, non-Blazor endpoints (F-5, build decision): writing or clearing the auth cookie needs a real
/// HTTP response, which an interactive Blazor Server circuit does not have. The interactive `/sign-in`
/// page does the actual credential check and only navigates here (`forceLoad: true`) to finish.
/// </summary>
public static class AccountEndpoints
{
    /// <summary>The sign-out reason that sends the visitor to sign in again with the "session ended" alert (B-3, BR5).</summary>
    public const string SessionEndedReason = "session-ended";

    /// <summary>The query that makes `/sign-in` show the "session ended" alert (B-3, BR5).</summary>
    public const string SessionEndedSignInPath = "/sign-in?session=ended";

    /// <summary>The sign-out reason that follows an account erasure (F-10, Screens).</summary>
    public const string AccountErasedReason = "account-erased";

    /// <summary>The query that makes `/sign-in` show the farewell alert (F-10, AC13).</summary>
    public const string AccountErasedSignInPath = "/sign-in?account=erased";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/account");

        group.MapGet("/sign-in-complete", CompleteSignInAsync).WithName("CompleteSignIn");
        group.MapGet("/sign-out", SignOutAsync).WithName("WebSignOut");
        group.MapGet("/profile-applied", ApplyProfileAsync).WithName("ApplyProfile");

        return endpoints;
    }

    /// <summary>B-3, BR1: the tokens go to the server-side store; the cookie gets the identity and the store key only.</summary>
    private static async Task<IResult> CompleteSignInAsync(
        string ticket,
        HttpContext context,
        SignInTicketStore tickets,
        IWebSessionStore sessions,
        TimeProvider timeProvider)
    {
        if (!tickets.TryConsume(ticket, out var signIn))
        {
            return Results.Redirect($"/sign-in?error={IdentityErrorCodes.InvalidCredentials}");
        }

        // A browser that signs in again over a live cookie leaves no orphan entry behind.
        var previousWebSessionId = context.User.FindFirstValue(WebAuthClaims.WebSessionId);
        if (previousWebSessionId is not null)
        {
            await sessions.RemoveAsync(previousWebSessionId, context.RequestAborted);
        }

        var now = timeProvider.GetUtcNow();
        var webSessionId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await sessions.SaveAsync(
            webSessionId,
            new WebSession(
                signIn.SessionJti,
                signIn.AccessToken,
                signIn.RefreshToken,
                now.Add(signIn.AccessTokenLifetime),
                signIn.Permissions,
                now,
                now.Add(TokenLifetimes.RefreshToken)),
            context.RequestAborted);

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, signIn.Subject));
        identity.AddClaim(new Claim(ClaimTypes.Email, signIn.Email));
        if (!string.IsNullOrWhiteSpace(signIn.DisplayName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, signIn.DisplayName));
        }

        identity.AddClaim(new Claim(WebAuthClaims.WebSessionId, webSessionId));

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // F-8 BR5: the profile's language wins over whatever this browser had.
        CultureCookie.Write(context, signIn.PreferredLanguage);
        return Results.LocalRedirect("/");
    }

    /// <summary>
    /// F-8 BR8: after a profile save, the cookie gets the saved display name and the culture cookie the saved
    /// language. A full page load, because a Blazor circuit can write neither and its culture is fixed when it starts.
    /// </summary>
    private static async Task<IResult> ApplyProfileAsync(
        string? redirectUri,
        HttpContext context,
        WebSessionTokenAccessor tokens,
        IdentityApiClient api)
    {
        var target = SupportedCultures.SafeLocalPath(redirectUri);
        var accessToken = context.User.Identity?.IsAuthenticated == true
            ? await tokens.GetAccessTokenAsync(context.User, context.RequestAborted)
            : null;
        var profile = accessToken is null ? null : (await api.GetProfileAsync(accessToken, context.RequestAborted)).Value;
        if (profile is null)
        {
            return Results.LocalRedirect(target);
        }

        // The same claims sign-in writes (permissions are added per request, never stored), with the new name,
        // and the cookie's original issue and expiry: a save does not extend the sign-in.
        var identity = new ClaimsIdentity(
            context.User.Claims.Where(claim => claim.Type is not (ClaimTypes.Name or WebAuthClaims.Permission)),
            CookieAuthenticationDefaults.AuthenticationScheme);
        if (!string.IsNullOrWhiteSpace(profile.FullName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, profile.FullName));
        }

        var current = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), current.Properties);
        CultureCookie.Write(context, profile.PreferredLanguage);
        return Results.LocalRedirect(target);
    }

    /// <summary>
    /// F-5 BR6: best-effort server-side revocation, then the stored session and the cookie clear regardless.
    /// With <see cref="SessionEndedReason"/> the session is already gone at the Api (B-3, BR5): no call,
    /// and the visitor lands on sign-in with the alert. <see cref="AccountErasedReason"/> works the same
    /// way after an erasure (F-10), with the farewell alert instead.
    /// </summary>
    private static async Task<IResult> SignOutAsync(string? reason, HttpContext context, AuthClient authClient, WebSessionTokenAccessor tokens)
    {
        var sessionEnded = reason == SessionEndedReason;
        var accountErased = reason == AccountErasedReason;
        var webSessionId = context.User.FindFirstValue(WebAuthClaims.WebSessionId);
        try
        {
            // F-10 BR10: the erasure already revoked every session at the Api, so there is nothing to call.
            var session = sessionEnded || accountErased || webSessionId is null
                ? null
                : await tokens.GetFreshAsync(webSessionId, context.RequestAborted);
            if (session is not null)
            {
                await authClient.SignOutAsync(session.AccessToken, context.RequestAborted);
            }
        }
        finally
        {
            // Whatever the Api did, this browser's session is gone.
            if (webSessionId is not null)
            {
                await tokens.EndAsync(webSessionId, CancellationToken.None);
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        if (accountErased)
        {
            return Results.LocalRedirect(AccountErasedSignInPath);
        }

        return Results.LocalRedirect(sessionEnded ? SessionEndedSignInPath : "/");
    }
}
