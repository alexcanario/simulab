using Microsoft.AspNetCore.Authentication;
using Simulab.Identity.Contracts;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// The Web host's two plain endpoints of a Google sign-in (F-20): a Blazor circuit can neither redirect the browser to
/// Google nor read the answer, so the pages only navigate here with a full load. Mapped only while the feature is on
/// (BR1): off, both answer 404.
/// </summary>
public static class GoogleAccountEndpoints
{
    public const string StartPath = "/account/google/start";

    public const string CompletePath = "/account/google/complete";

    /// <summary>The confirmation page of a first Google sign-in (BR7), with the ticket in its query.</summary>
    public const string SignUpPath = "/sign-up/google";

    /// <summary>The query key that hands a two-factor challenge to the code step of <c>/sign-in</c> (BR6).</summary>
    public const string CodeStepQuery = "code-step";

    public static IEndpointRouteBuilder MapGoogleAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(StartPath, Start).WithName("StartGoogleSignIn");
        endpoints.MapGet(CompletePath, CompleteAsync).WithName("CompleteGoogleSignIn");

        return endpoints;
    }

    private static IResult Start() =>
        Results.Challenge(new AuthenticationProperties { RedirectUri = CompletePath }, [GoogleSignInSettings.Scheme]);

    /// <summary>
    /// Google's answer is read once from the external cookie and the cookie cleared; the ID token goes to the Api, which
    /// decides (BR2). Each outcome leaves for its page: signed in, the code step, the confirmation, or sign-in with the reason.
    /// </summary>
    private static async Task<IResult> CompleteAsync(
        HttpContext context,
        AuthClient auth,
        SignInHandOff handOff,
        GoogleSignUpTickets signUps,
        CodeStepTickets codeSteps)
    {
        var external = await context.AuthenticateAsync(GoogleSignInSettings.ExternalScheme);
        await context.SignOutAsync(GoogleSignInSettings.ExternalScheme);

        var idToken = external.Succeeded ? external.Properties?.GetTokenValue("id_token") : null;
        if (string.IsNullOrEmpty(idToken))
        {
            return SignInWithError(IdentityErrorCodes.GoogleSignInExpired);
        }

        var result = await auth.SignInWithGoogleAsync(idToken, context.RequestAborted);

        if (result.IsSuccess)
        {
            var completion = await handOff.CompletionPathAsync(result, context.RequestAborted);
            return completion is null ? SignInWithError(Components.Ui.ErrorText.UnexpectedCode) : Results.LocalRedirect(completion);
        }

        if (result.NeedsTotpCode)
        {
            var ticket = codeSteps.Issue(new CodeStepTicket(result.Challenge!));
            return Results.LocalRedirect($"/sign-in?{CodeStepQuery}={ticket}");
        }

        if (result.NeedsGoogleSignUp)
        {
            var ticket = signUps.Issue(new GoogleSignUpTicket(idToken, result.GoogleEmail!, result.GoogleName));
            return Results.LocalRedirect($"{SignUpPath}?ticket={ticket}");
        }

        return SignInWithError(result.ErrorCode ?? Components.Ui.ErrorText.UnexpectedCode);
    }

    private static IResult SignInWithError(string code) =>
        Results.LocalRedirect($"/sign-in?error={Uri.EscapeDataString(code)}");
}
