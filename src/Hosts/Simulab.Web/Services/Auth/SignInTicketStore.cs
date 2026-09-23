namespace Simulab.Web.Services.Auth;

/// <summary>Everything the completion endpoint needs to write the auth cookie, carried by one ticket.</summary>
public sealed record SignInTicket(
    string Subject,
    string Email,
    string? DisplayName,
    string SessionJti,
    string AccessToken,
    string RefreshToken,
    TimeSpan AccessTokenLifetime,
    IReadOnlyList<string> Permissions,
    string? PreferredLanguage = null);

/// <summary>
/// A single-use, short-lived relay between the interactive sign-in page and the plain endpoint that
/// writes the auth cookie (F-5, build decision: a Blazor Interactive Server circuit has no open HTTP
/// response to call <c>HttpContext.SignInAsync</c> on). Tokens never sit in a URL or a log: the ticket
/// id is the only thing that travels there, and it is consumed at most once (<see cref="SingleUseTickets{T}.TryConsume"/>).
/// Like every ticket store here it lives in this process: the Web host runs as one instance (docs/infra.md).
/// </summary>
public sealed class SignInTicketStore(TimeProvider timeProvider)
    : SingleUseTickets<SignInTicket>(timeProvider, TimeSpan.FromSeconds(30));
