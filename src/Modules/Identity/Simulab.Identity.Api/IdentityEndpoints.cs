using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.Account;
using Simulab.Identity.Application.Passwords;
using Simulab.Identity.Application.Profile;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Api;

/// <summary>
/// The module's HTTP surface. Most of it is anonymous (F-4 BR16, F-7 BR12): the calls a visitor makes
/// before there is an account to authorise, or when the password is lost.
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

        // F-7: password recovery is anonymous (BR12) and answers the same whether the account exists.
        group.MapPost("/password-reset-requests", RequestPasswordResetAsync).WithName("RequestPasswordReset");

        group.MapPost("/password-reset-token-checks", CheckPasswordResetTokenAsync).WithName("CheckPasswordResetToken");

        group.MapPost("/password-resets", ResetPasswordAsync).WithName("ResetPassword");

        // F-7 BR12: the caller's own account only, so no permission is involved.
        group.MapPost("/password-changes", ChangePasswordAsync)
            .WithName("ChangePassword")
            .RequireAuthorization();

        // BR9: the only endpoints here that require a signed-in caller (F-5).
        group.MapPost("/sign-out", SignOutAsync)
            .WithName("SignOut")
            .RequireAuthorization();

        group.MapGet("/session", GetSessionAsync)
            .WithName("GetSession")
            .RequireAuthorization();

        // F-10 BR1: the caller's own account, found by the token subject; no permission is involved.
        group.MapPost("/account-erasures", EraseAccountAsync)
            .WithName("EraseAccount")
            .RequireAuthorization();

        // F-16 BR1: the caller's own data, found by the token subject; no permission is involved.
        group.MapPost("/data-exports", ExportDataAsync)
            .WithName("ExportData")
            .RequireAuthorization();

        // F-8 BR1: the caller's own profile, found by the token subject; no permission is involved.
        var profile = group.MapGroup("/profile").RequireAuthorization();
        profile.MapGet(string.Empty, GetProfileAsync).WithName("GetProfile");
        profile.MapPut(string.Empty, UpdateProfileAsync).WithName("UpdateProfile");
        profile.MapPut("/preferred-language", UpdatePreferredLanguageAsync).WithName("UpdatePreferredLanguage");

        // F-9: the role management back office, every route behind identity.roles.manage (BR10).
        group.MapRoleAdministrationEndpoints();

        // F-11 BR12: two-factor routes exist only while the feature is on.
        if (endpoints.ServiceProvider.GetRequiredService<IOptions<TotpOptions>>().Value.TotpEnabled)
        {
            group.MapTotpEndpoints();
        }

        return endpoints;
    }

    /// <summary>
    /// Lets the Web read its own just-issued access token's claims without decoding the token itself
    /// (F-5, build decision: the token may be encrypted, and decoding it is not the Web's job either way).
    /// </summary>
    private static async Task<IResult> GetSessionAsync(ClaimsPrincipal user, IPermissionQueryService permissions, ProfileHandler profiles, CancellationToken cancellationToken)
    {
        var subject = user.FindFirstValue(OpenIddictConstants.Claims.Subject);
        var email = user.FindFirstValue(OpenIddictConstants.Claims.Email);
        var sessionJti = user.FindFirstValue(SessionClaims.SessionJti);

        if (subject is null || email is null || sessionJti is null || !Guid.TryParse(subject, out var userId))
        {
            return Problem(new Error(IdentityErrorCodes.RefreshTokenInvalid, ErrorKind.Validation), StatusCodes.Status400BadRequest);
        }

        var effective = await permissions.GetEffectivePermissionsAsync(userId, cancellationToken);

        // F-8 BR5, BR8: the name and language as they are now, so a sign-in on any device starts with them.
        var profile = await profiles.GetAsync(userId);
        return Results.Ok(new SessionInfoResponse(subject, email, sessionJti, effective.ToList(), profile?.FullName, profile?.PreferredLanguage));
    }

    private static async Task<IResult> GetProfileAsync(ClaimsPrincipal user, ProfileHandler handler)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var profile = await handler.GetAsync(userId);
        return profile is null ? Results.Unauthorized() : Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfileAsync(UpdateProfileRequest request, ClaimsPrincipal user, ProfileHandler handler)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryGetUserId(user, out var userId)
            ? ProfileAnswer(await handler.UpdateAsync(userId, request.FullName, request.PreferredLanguage))
            : Results.Unauthorized();
    }

    private static async Task<IResult> UpdatePreferredLanguageAsync(UpdatePreferredLanguageRequest request, ClaimsPrincipal user, ProfileHandler handler)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryGetUserId(user, out var userId)
            ? ProfileAnswer(await handler.UpdatePreferredLanguageAsync(userId, request.PreferredLanguage))
            : Results.Unauthorized();
    }

    private static IResult ProfileAnswer(Result result) =>
        result.IsSuccess ? Results.NoContent()
        : result.Error!.Kind == ErrorKind.NotFound ? Results.Unauthorized()
        : Problem(result.Error);

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(OpenIddictConstants.Claims.Subject), out userId);

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
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "register"), IdentityRateLimits.RegistrationsPerHour, IdentityRateLimits.Window))
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
            clientAddress.Of(context));

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
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "resend"), IdentityRateLimits.ResendsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.VerificationRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        await handler.HandleAsync(request.Email, cancellationToken);
        return Results.Accepted();
    }

    /// <summary>F-7 BR1: always 202 when the client is within its limit; the per-account limit is silent (BR3).</summary>
    private static async Task<IResult> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        HttpContext context,
        RequestPasswordResetHandler handler,
        ClientRateLimiter rateLimiter,
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "password-reset-request"), IdentityRateLimits.PasswordResetRequestsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.PasswordResetRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        await handler.HandleAsync(request.Email, cancellationToken);
        return Results.Accepted();
    }

    /// <summary>F-7: the reset page checks its link on load, without using it. Shares the reset's per-client limit.</summary>
    private static async Task<IResult> CheckPasswordResetTokenAsync(
        PasswordResetTokenCheckRequest request,
        HttpContext context,
        CheckPasswordResetTokenHandler handler,
        ClientRateLimiter rateLimiter,
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "password-reset"), IdentityRateLimits.PasswordResetsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.PasswordResetRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        return await handler.HandleAsync(request.Token, cancellationToken) switch
        {
            PasswordResetTokenStatus.Valid => Results.NoContent(),
            PasswordResetTokenStatus.Expired => Problem(new Error(IdentityErrorCodes.PasswordResetExpired, ErrorKind.BusinessRule), StatusCodes.Status410Gone),
            _ => Problem(new Error(IdentityErrorCodes.PasswordResetInvalid, ErrorKind.Validation))
        };
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        HttpContext context,
        ResetPasswordHandler handler,
        ClientRateLimiter rateLimiter,
        ClientAddress clientAddress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "password-reset"), IdentityRateLimits.PasswordResetsPerHour, IdentityRateLimits.Window))
        {
            return Problem(new Error(IdentityErrorCodes.PasswordResetRateLimited, ErrorKind.BusinessRule), StatusCodes.Status429TooManyRequests);
        }

        var result = await handler.HandleAsync(request.Token, request.NewPassword, cancellationToken);
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.Error!.Code == IdentityErrorCodes.PasswordResetExpired
            ? Problem(result.Error, StatusCodes.Status410Gone)
            : Problem(result.Error);
    }

    /// <summary>F-7 BR8-BR10: the caller's own password; a lockout answers 423 with the remaining seconds.</summary>
    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal user,
        ChangePasswordHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = user.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.HandleAsync(
            userId,
            user.FindFirstValue(SessionClaims.SessionJti),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        if (result.Error!.Code != IdentityErrorCodes.AccountLocked)
        {
            return Problem(result.Error);
        }

        var seconds = int.TryParse(result.Error.Detail, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        return Problem(result.Error with { Detail = null }, StatusCodes.Status423Locked, ("retryAfterSeconds", seconds));
    }

    /// <summary>
    /// F-10 BR1, BR2: the caller's own account, confirmed with the current password. A lockout answers 423
    /// with the remaining seconds, exactly as a password change does.
    /// </summary>
    private static async Task<IResult> EraseAccountAsync(
        EraseAccountRequest request,
        ClaimsPrincipal user,
        EraseAccountHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, request.CurrentPassword, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        if (result.Error!.Code != IdentityErrorCodes.AccountLocked)
        {
            return Problem(result.Error);
        }

        var seconds = int.TryParse(result.Error.Detail, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        return Problem(result.Error with { Detail = null }, StatusCodes.Status423Locked, ("retryAfterSeconds", seconds));
    }

    /// <summary>
    /// F-16 BR1, BR2, BR8: the caller's own data as one JSON attachment, confirmed with the current password. A
    /// lockout answers 423 with the remaining seconds, exactly as an erasure does.
    /// </summary>
    private static async Task<IResult> ExportDataAsync(
        DataExportRequest request,
        ClaimsPrincipal user,
        ExportDataHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, request.CurrentPassword, cancellationToken);

        if (result.IsSuccess)
        {
            var export = result.Value;
            var fileName = $"simulab-my-data-{export.ExportedAt.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}.json";
            var body = JsonSerializer.SerializeToUtf8Bytes(export, AppJson.Options);
            return Results.File(body, "application/json", fileName);
        }

        if (result.Error!.Code != IdentityErrorCodes.AccountLocked)
        {
            return Problem(result.Error);
        }

        var seconds = int.TryParse(result.Error.Detail, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        return Problem(result.Error with { Detail = null }, StatusCodes.Status423Locked, ("retryAfterSeconds", seconds));
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

    private static bool TryParseTopic(string topic, out LegalTopic parsed) =>
        Enum.TryParse(topic, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    /// <summary>RFC 9457 problem details plus the stable code the UI turns into text (rule: api-contracts).</summary>
    internal static IResult Problem(Error error, int? status = null, params (string Name, object Value)[] extensions)
    {
        var problem = new ProblemDetails
        {
            Status = status ?? StatusFor(error.Kind),
            Title = error.Code,
            Detail = error.Detail,
            Extensions = { ["code"] = error.Code }
        };

        foreach (var (name, value) in extensions)
        {
            problem.Extensions[name] = value;
        }

        return Results.Problem(problem);
    }

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
