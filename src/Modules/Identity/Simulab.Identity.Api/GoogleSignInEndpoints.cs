using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        return identity;
    }

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
        if (!rateLimiter.TryAcquire(clientAddress.KeyFor(context, "register"), IdentityRateLimits.RegistrationsPerHour, IdentityRateLimits.Window))
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
