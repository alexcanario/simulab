using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Simulab.Identity.Contracts;

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

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/account");

        group.MapGet("/sign-in-complete", CompleteSignInAsync).WithName("CompleteSignIn");
        group.MapGet("/sign-out", SignOutAsync).WithName("WebSignOut");

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
                now),
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
        return Results.LocalRedirect("/");
    }

    /// <summary>
    /// F-5 BR6: best-effort server-side revocation, then the stored session and the cookie clear regardless.
    /// With <see cref="SessionEndedReason"/> the session is already gone at the Api (B-3, BR5): no call,
    /// and the visitor lands on sign-in with the alert.
    /// </summary>
    private static async Task<IResult> SignOutAsync(string? reason, HttpContext context, AuthClient authClient, WebSessionTokenAccessor tokens)
    {
        var sessionEnded = reason == SessionEndedReason;
        var webSessionId = context.User.FindFirstValue(WebAuthClaims.WebSessionId);
        if (webSessionId is not null)
        {
            var session = sessionEnded ? null : await tokens.GetFreshAsync(webSessionId, context.RequestAborted);
            if (session is not null)
            {
                await authClient.SignOutAsync(session.AccessToken, context.RequestAborted);
            }

            await tokens.EndAsync(webSessionId, context.RequestAborted);
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect(sessionEnded ? "/sign-in?session=ended" : "/");
    }
}
