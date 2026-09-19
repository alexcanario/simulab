using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Api.Authorization;

/// <summary>
/// Checks the caller's effective permissions against the database (F-6, BR3) - never a claim, so a
/// permission revoked after the access token was issued still takes effect on the next request.
/// </summary>
public sealed class PermissionAuthorizationHandler(IPermissionQueryService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject = context.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return;
        }

        var effective = await permissions.GetEffectivePermissionsAsync(userId, CancellationToken.None);
        if (effective.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
