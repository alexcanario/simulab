using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Api;

/// <summary>
/// Two-factor on the caller's own account (F-11 BR1): the account is the token subject, so no permission is
/// involved and no id travels in the route or the body. Mapped only while <c>Identity:TotpEnabled</c> is true
/// (BR12): with the feature off, every route here answers 404.
/// </summary>
public static class TotpEndpoints
{
    public static RouteGroupBuilder MapTotpEndpoints(this RouteGroupBuilder identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var totp = identity.MapGroup("/totp").RequireAuthorization();

        totp.MapGet(string.Empty, GetStatusAsync).WithName("GetTotpStatus");
        totp.MapDelete(string.Empty, DisableAsync).WithName("DisableTotp");
        totp.MapPost("/enrolments", StartAsync).WithName("StartTotpEnrolment");
        totp.MapPost("/enrolments/confirmations", ConfirmAsync).WithName("ConfirmTotpEnrolment");
        totp.MapPost("/recovery-codes", RegenerateRecoveryCodesAsync).WithName("RegenerateRecoveryCodes");

        return identity;
    }

    private static async Task<IResult> GetStatusAsync(ClaimsPrincipal user, TotpAccountHandler handler)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var status = await handler.GetStatusAsync(userId);
        return status is null ? Results.Unauthorized() : Results.Ok(status);
    }

    private static async Task<IResult> StartAsync(ClaimsPrincipal user, TotpAccountHandler handler) =>
        TryGetUserId(user, out var userId)
            ? Answer(await handler.StartAsync(userId), Results.Ok)
            : Results.Unauthorized();

    private static async Task<IResult> ConfirmAsync(ConfirmTotpRequest request, ClaimsPrincipal user, TotpAccountHandler handler)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryGetUserId(user, out var userId)
            ? Answer(await handler.ConfirmAsync(userId, request.Code), Results.Ok)
            : Results.Unauthorized();
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync(RegenerateRecoveryCodesRequest request, ClaimsPrincipal user, TotpAccountHandler handler)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryGetUserId(user, out var userId)
            ? Answer(await handler.RegenerateRecoveryCodesAsync(userId, request.Code), Results.Ok)
            : Results.Unauthorized();
    }

    private static async Task<IResult> DisableAsync([FromBody] DisableTotpRequest request, ClaimsPrincipal user, TotpAccountHandler handler)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.DisableAsync(userId, request.CurrentPassword, request.Code);
        return result.IsSuccess ? Results.NoContent() : Failure(result.Error!);
    }

    private static IResult Answer<T>(Result<T> result, Func<T, IResult> success) =>
        result.IsSuccess ? success(result.Value) : Failure(result.Error!);

    /// <summary>A lockout answers 423 with the remaining seconds, as a password change does (F-7); a vanished account is 401.</summary>
    private static IResult Failure(Error error)
    {
        if (error.Kind == ErrorKind.NotFound)
        {
            return Results.Unauthorized();
        }

        if (error.Code != IdentityErrorCodes.AccountLocked)
        {
            return IdentityEndpoints.Problem(error);
        }

        var seconds = int.TryParse(error.Detail, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        return IdentityEndpoints.Problem(error with { Detail = null }, StatusCodes.Status423Locked, ("retryAfterSeconds", seconds));
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(OpenIddictConstants.Claims.Subject), out userId);
}
