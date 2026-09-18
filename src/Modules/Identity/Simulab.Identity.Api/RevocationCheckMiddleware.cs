using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Api;

/// <summary>
/// BR7: checked on every request, right after authentication. An access token is a self-contained JWT
/// that OpenIddict validates on its own signature and expiry alone, so sign-out (BR6) would otherwise
/// have no effect until the token's own 15-minute lifetime runs out.
/// </summary>
public sealed class RevocationCheckMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRefreshSessionStore sessions)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sessionJti = context.User.FindFirst(SessionClaims.SessionJti)?.Value;
        if (!string.IsNullOrEmpty(sessionJti) && await sessions.IsAccessTokenRevokedAsync(sessionJti, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = IdentityErrorCodes.TokenRevoked,
                    Extensions = { ["code"] = IdentityErrorCodes.TokenRevoked }
                },
                AppJson.Options,
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
