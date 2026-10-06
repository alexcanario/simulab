using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.GoogleSignIn;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Identity.Api;

/// <summary>
/// The confirmation of a first Google sign-in (F-20 UC1, BR7). Anonymous, like the password sign-up: the proof is the
/// Google ID token, which the Api checks itself (BR2). Mapped only while <c>Identity:GoogleSignInEnabled</c> is true
/// (BR1): with the feature off, the route answers 404.
/// </summary>
public static class GoogleSignInEndpoints
{
    public static RouteGroupBuilder MapGoogleSignInEndpoints(this RouteGroupBuilder identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        identity.MapPost("/google-registrations", RegisterAsync).WithName("RegisterWithGoogle");

        // F-29 BR1: the caller's own link, found by the token subject; no permission and no user id in the
        // route. They live in this group, so the switch that maps it already gives AC17 its 404.
        identity.MapGet("/google-links", GetLinkAsync).WithName("GetGoogleLink").RequireAuthorization();
        identity.MapPost("/google-links", LinkAsync).WithName("LinkGoogle").RequireAuthorization();
        identity.MapPost("/google-link-removals", UnlinkAsync).WithName("UnlinkGoogle").RequireAuthorization();

        return identity;
    }

    /// <summary>F-29 BR12: what the Security page shows — linked or not, and the address when one is stored.</summary>
    private static async Task<IResult> GetLinkAsync(
        ClaimsPrincipal user,
        GoogleLinkHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.GetAsync(userId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    /// <summary>F-29 BR2 to BR7: links the caller's own account to the identity of a checked ID token.</summary>
    private static async Task<IResult> LinkAsync(
        GoogleLinkRequest request,
        ClaimsPrincipal user,
        GoogleLinkHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.LinkAsync(userId, request.IdToken, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    /// <summary>
    /// F-29 BR8 to BR10: removes the caller's own link, confirmed with the current password. A lockout
    /// answers 423 with the remaining seconds, exactly as an erasure does.
    /// </summary>
    private static async Task<IResult> UnlinkAsync(
        GoogleLinkRemovalRequest request,
        ClaimsPrincipal user,
        GoogleLinkHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.UnlinkAsync(userId, request.CurrentPassword, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        if (result.Error!.Code != IdentityErrorCodes.AccountLocked)
        {
            return Problem(result.Error);
        }

        var seconds = int.TryParse(result.Error.Detail, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        return Problem(result.Error with { Detail = null }, StatusCodes.Status423Locked, ("retryAfterSeconds", seconds));
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(OpenIddictConstants.Claims.Subject), out userId);

    /// <summary>201 when the account exists now; the Web host then runs the <c>google</c> grant to sign it in.</summary>
    private static async Task<IResult> RegisterAsync(
        GoogleRegistrationRequest request,
        HttpContext context,
        RegisterGoogleUserHandler handler,
        ClientRateLimiter rateLimiter,
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // BR10: the same per-client-address budget as the password sign-up; both count on it.
        if (!await rateLimiter.TryAcquireAsync(clientAddress.KeyFor(context, "register"), IdentityRateLimits.RegistrationsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.RegistrationRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        var command = new RegisterGoogleUserCommand(
            request.IdToken,
            request.DeclaresAdult,
            request.AcceptsTerms,
            request.AcceptsPrivacy,
            request.TermsVersion,
            request.PrivacyVersion,
            RequestLocale.From(context.Request),
            request.FullName,
            clientAddress.Of(context));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? Results.StatusCode(StatusCodes.Status201Created) : Problem(result.Error!);
    }
}
