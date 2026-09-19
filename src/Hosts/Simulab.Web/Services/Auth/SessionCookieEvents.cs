using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// B-3, BR4: every request that carries the auth cookie is checked against the server-side session. A
/// page load always asks the Api; other requests (assets, the circuit's own) at most once a minute. An
/// ended session rejects the principal and clears the cookie; a live one gets its current permissions.
/// </summary>
public sealed class SessionCookieEvents : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        var webSessionId = principal?.FindFirstValue(WebAuthClaims.WebSessionId);
        var accessor = context.HttpContext.RequestServices.GetRequiredService<WebSessionTokenAccessor>();

        var session = webSessionId is null
            ? null
            : await accessor.CheckAsync(webSessionId, IsPageLoad(context.HttpContext.Request), context.HttpContext.RequestAborted);

        if (session is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        context.ReplacePrincipal(WithPermissions(principal!, session.Permissions));
    }

    /// <summary>A browser navigation to a page, as opposed to an asset or a script's own request.</summary>
    private static bool IsPageLoad(HttpRequest request) =>
        string.Equals(request.Headers["Sec-Fetch-Mode"], "navigate", StringComparison.OrdinalIgnoreCase)
        || request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

    private static ClaimsPrincipal WithPermissions(ClaimsPrincipal principal, IReadOnlyList<string> permissions)
    {
        var identity = new ClaimsIdentity(
            principal.Claims.Where(claim => claim.Type != WebAuthClaims.Permission),
            principal.Identity?.AuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role);
        identity.AddClaims(permissions.Select(permission => new Claim(WebAuthClaims.Permission, permission)));
        return new ClaimsPrincipal(identity);
    }
}
