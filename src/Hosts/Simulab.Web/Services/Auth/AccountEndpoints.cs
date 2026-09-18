using System.Security.Claims;
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
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/account");

        group.MapGet("/sign-in-complete", CompleteSignInAsync).WithName("CompleteSignIn");
        group.MapGet("/sign-out", SignOutAsync).WithName("WebSignOut");

        return endpoints;
    }

    private static async Task<IResult> CompleteSignInAsync(string ticket, HttpContext context, SignInTicketStore tickets)
    {
        if (!tickets.TryConsume(ticket, out var signIn))
        {
            return Results.Redirect($"/sign-in?error={IdentityErrorCodes.InvalidCredentials}");
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, signIn.Subject));
        identity.AddClaim(new Claim(ClaimTypes.Email, signIn.Email));
        if (!string.IsNullOrWhiteSpace(signIn.DisplayName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, signIn.DisplayName));
        }

        identity.AddClaim(new Claim(SessionClaims.SessionJti, signIn.SessionJti));
        identity.AddClaim(new Claim(WebAuthClaims.AccessToken, signIn.AccessToken));
        identity.AddClaim(new Claim(WebAuthClaims.RefreshToken, signIn.RefreshToken));

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.LocalRedirect("/");
    }

    /// <summary>BR6: best-effort server-side revocation, then the cookie clears regardless.</summary>
    private static async Task<IResult> SignOutAsync(HttpContext context, AuthClient authClient)
    {
        var accessToken = context.User.FindFirstValue(WebAuthClaims.AccessToken);
        if (accessToken is not null)
        {
            await authClient.SignOutAsync(accessToken);
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/");
    }
}
