using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Api.Authorization;

/// <summary>
/// A failed permission check answers with the stable code `identity.forbidden` (F-6, BR4), the same
/// RFC 9457 shape every other error in this module uses, instead of the framework's empty 403 body.
/// </summary>
public sealed class PermissionForbiddenResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = IdentityErrorCodes.Forbidden,
                    Extensions = { ["code"] = IdentityErrorCodes.Forbidden }
                },
                AppJson.Options,
                context.RequestAborted);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
