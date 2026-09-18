using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Api;

/// <summary>
/// The module's HTTP surface. Everything here is anonymous (BR16): these are the calls a visitor makes
/// before there is an account to authorise.
/// </summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/identity").WithTags("Identity");

        group.MapPost("/registrations", RegisterAsync).WithName("Register");

        group.MapPost("/email-verifications", VerifyEmailAsync)
            .WithName("VerifyEmail");

        group.MapPost("/email-verifications/resend", ResendVerificationAsync).WithName("ResendVerification");

        group.MapGet("/legal-documents/{topic}", GetLegalDocumentAsync)
            .WithName("GetLegalDocument");

        // BR9: the only endpoint here that requires a signed-in caller (F-5).
        group.MapPost("/sign-out", SignOutAsync)
            .WithName("SignOut")
            .RequireAuthorization();

        return endpoints;
    }

    /// <summary>BR6: drops the caller's own session and revokes its access token, regardless of whether either call finds anything to act on.</summary>
    private static async Task<IResult> SignOutAsync(ClaimsPrincipal user, IRefreshSessionStore sessions, CancellationToken cancellationToken)
    {
        var sessionJti = user.FindFirstValue(SessionClaims.SessionJti);
        if (!string.IsNullOrEmpty(sessionJti))
        {
            await sessions.RemoveAsync(sessionJti, cancellationToken);
            await sessions.RevokeAccessTokenAsync(sessionJti, TokenLifetimes.AccessToken, cancellationToken);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Always 202 when the request is well formed, whether the account was created or the address was
    /// already registered (BR4). The client cannot tell the two apart, and neither can an attacker.
    /// </summary>
    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext context,
        RegisterUserHandler handler,
        ClientRateLimiter rateLimiter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(ClientKey(context, "register"), IdentityRateLimits.RegistrationsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.RegistrationRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.DeclaresAdult,
            request.AcceptsTerms,
            request.AcceptsPrivacy,
            request.TermsVersion,
            request.PrivacyVersion,
            RequestLocale.From(context.Request),
            request.FullName,
            context.Connection.RemoteIpAddress?.ToString());

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? Results.Accepted() : Problem(result.Error!);
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        VerifyEmailHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outcome = await handler.HandleAsync(request.Token, cancellationToken);

        return outcome switch
        {
            VerificationOutcome.Verified => Results.Ok(new VerifyEmailResponse(outcome)),
            VerificationOutcome.Expired => Problem(
                new Error(IdentityErrorCodes.VerificationExpired, ErrorKind.BusinessRule),
                StatusCodes.Status410Gone),
            _ => Problem(new Error(IdentityErrorCodes.VerificationInvalid, ErrorKind.Validation))
        };
    }

    /// <summary>
    /// Always 202 (BR11). A throttled address gets the same answer as an unknown one, so the cooldown
    /// cannot be used to find out which addresses are registered.
    /// </summary>
    private static async Task<IResult> ResendVerificationAsync(
        ResendVerificationRequest request,
        HttpContext context,
        ResendVerificationHandler handler,
        ClientRateLimiter rateLimiter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(ClientKey(context, "resend"), IdentityRateLimits.ResendsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.VerificationRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        await handler.HandleAsync(request.Email, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> GetLegalDocumentAsync(
        string topic,
        HttpContext context,
        Simulab.Identity.Application.Abstractions.ILegalDocumentProvider documents,
        CancellationToken cancellationToken)
    {
        if (!TryParseTopic(topic, out var parsed))
        {
            return Problem(new Error(IdentityErrorCodes.LegalDocumentNotFound, ErrorKind.NotFound), StatusCodes.Status404NotFound);
        }

        var document = await documents.GetCurrentAsync(parsed, RequestLocale.From(context.Request), cancellationToken);

        return document is null
            ? Problem(new Error(IdentityErrorCodes.LegalDocumentNotFound, ErrorKind.NotFound), StatusCodes.Status404NotFound)
            : Results.Ok(document);
    }

    // Behind a proxy this is the proxy's address until forwarded headers are configured: a deployment
    // concern, and the limit still holds as one bucket instead of many.
    private static string ClientKey(HttpContext context, string scope) =>
        $"{scope}:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private static bool TryParseTopic(string topic, out LegalTopic parsed) =>
        Enum.TryParse(topic, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    /// <summary>RFC 9457 problem details plus the stable code the UI turns into text (rule: api-contracts).</summary>
    private static IResult Problem(Error error, int? status = null) =>
        Results.Problem(new ProblemDetails
        {
            Status = status ?? StatusFor(error.Kind),
            Title = error.Code,
            Detail = error.Detail,
            Extensions = { ["code"] = error.Code }
        });

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
