using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>F-29 BR2: where the Security page lands after a link attempt, with its outcome in the query.</summary>
    public const string SecurityPath = "/account/security";

    /// <summary>F-29 BR2: the marker in the challenge's properties that tells a link from a sign-in.</summary>
    public const string LinkIntentItem = "simulab.link-intent";

    /// <summary>F-29 BR2: the link ticket's id, beside the marker, inside the same protected properties.</summary>
    public const string LinkTicketItem = "simulab.link-ticket";

    public static IEndpointRouteBuilder MapGoogleAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(StartPath, Start).WithName("StartGoogleSignIn");

        // F-29 BR2: the link intent is a POST of the same path, never the GET above - sign-in and sign-up
        // reach that one with a plain navigation. The form-bound parameter is what makes the antiforgery
        // middleware validate this endpoint at all; without it the POST would ship unprotected.
        endpoints.MapPost(StartPath, StartLink).WithName("StartGoogleLink").RequireAuthorization();
        endpoints.MapGet(CompletePath, CompleteAsync).WithName("CompleteGoogleSignIn");

        return endpoints;
    }

    private static IResult Start() =>
        Results.Challenge(new AuthenticationProperties { RedirectUri = CompletePath }, [GoogleSignInSettings.Scheme]);

    /// <summary>
    /// F-29 BR2: the signed-in account asks to link. The ticket id and the marker travel in the challenge's
    /// properties, which the OIDC handler protects into <c>state</c>, so they come back in the encrypted
    /// external cookie and never appear in a URL a page could read or a referrer could leak.
    /// </summary>
    private static IResult StartLink(
        [FromForm] string intent,
        HttpContext context,
        GoogleLinkTickets tickets)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tickets);

        if (!string.Equals(intent, LinkIntent, StringComparison.Ordinal)
            || !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.LocalRedirect(SecurityPath);
        }

        var properties = new AuthenticationProperties { RedirectUri = CompletePath };
        properties.Items[LinkIntentItem] = LinkIntent;
        properties.Items[LinkTicketItem] = tickets.Issue(new GoogleLinkTicket(userId));
        return Results.Challenge(properties, [GoogleSignInSettings.Scheme]);
    }

    /// <summary>The only value <c>intent</c> takes; anything else is not a link.</summary>
    public const string LinkIntent = "link";

    /// <summary>
    /// Google's answer is read once from the external cookie and the cookie cleared; the ID token goes to the Api, which
    /// decides (BR2). Each outcome leaves for its page: signed in, the code step, the confirmation, or sign-in with the reason.
    /// </summary>
    private static async Task<IResult> CompleteAsync(
        HttpContext context,
        AuthClient auth,
        SignInHandOff handOff,
        GoogleSignUpTickets signUps,
        CodeStepTickets codeSteps,
        GoogleLinkTickets tickets)
    {
        var external = await context.AuthenticateAsync(GoogleSignInSettings.ExternalScheme);
        await context.SignOutAsync(GoogleSignInSettings.ExternalScheme);

        var idToken = external.Succeeded ? external.Properties?.GetTokenValue("id_token") : null;

        // F-29 BR2: the marker decides. Absent, this is an F-20 sign-in and nothing here runs.
        var items = external.Properties?.Items;
        if (items is not null && items.TryGetValue(LinkIntentItem, out var intent) && intent == LinkIntent)
        {
            // F-29: the two services only the link branch needs are resolved here, not taken as endpoint
            // parameters. A parameter is built on every request, and `WebSessionTokenAccessor` reaches the
            // Redis-backed session store — which turned every F-20 sign-in through this callback into a 500
            // wherever Redis is not configured, the Web test host included.
            var services = context.RequestServices;
            return await CompleteLinkAsync(
                context,
                services.GetRequiredService<IdentityApiClient>(),
                services.GetRequiredService<WebSessionTokenAccessor>(),
                tickets,
                items,
                idToken);
        }

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
            // BR8 and the independent review: the ticket opens only in this browser, which keeps the secret in a cookie.
            var (secret, hash) = GoogleSignUpTicket.NewBinding();
            var ticket = signUps.Issue(new GoogleSignUpTicket(idToken, result.GoogleEmail!, result.GoogleName, hash));
            context.Response.Cookies.Append(GoogleSignUpTicket.BindingCookie, secret, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = SignUpPath,
                MaxAge = GoogleSignUpTickets.Lifetime,
            });
            return Results.LocalRedirect($"{SignUpPath}?ticket={ticket}");
        }

        return SignInWithError(result.ErrorCode ?? Components.Ui.ErrorText.UnexpectedCode);
    }

    /// <summary>
    /// F-29 BR2. Three refusals the Api cannot make, because it knows nothing of the ticket: the attempt
    /// expired, the session changed, or Google gave nothing back. Only then is the Api asked to link.
    /// </summary>
    private static async Task<IResult> CompleteLinkAsync(
        HttpContext context,
        IdentityApiClient api,
        WebSessionTokenAccessor tokens,
        GoogleLinkTickets tickets,
        IDictionary<string, string?> items,
        string? idToken)
    {
        items.TryGetValue(LinkTicketItem, out var ticketId);
        if (!tickets.TryConsume(ticketId, out var ticket))
        {
            // A Web restart empties them, so this is "start again", not a security refusal.
            return SecurityWithError(IdentityErrorCodes.GoogleLinkExpired);
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId != ticket.UserId)
        {
            return SecurityWithError(IdentityErrorCodes.GoogleLinkSessionChanged);
        }

        if (string.IsNullOrEmpty(idToken))
        {
            return SecurityWithError(IdentityErrorCodes.GoogleSignInExpired);
        }

        var accessToken = await tokens.GetAccessTokenAsync(context.User, context.RequestAborted);
        if (accessToken is null)
        {
            return SecurityWithError(IdentityErrorCodes.GoogleLinkSessionChanged);
        }

        var linked = await api.LinkGoogleAsync(accessToken, idToken, context.RequestAborted);
        return linked.IsSuccess
            ? Results.LocalRedirect($"{SecurityPath}?linked=1")
            : SecurityWithError(linked.ErrorCode ?? Components.Ui.ErrorText.UnexpectedCode);
    }

    private static IResult SignInWithError(string code) =>
        Results.LocalRedirect($"/sign-in?error={Uri.EscapeDataString(code)}");

    private static IResult SecurityWithError(string code) =>
        Results.LocalRedirect($"{SecurityPath}?error={Uri.EscapeDataString(code)}");
}
