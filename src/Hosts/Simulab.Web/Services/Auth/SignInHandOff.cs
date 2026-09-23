using Simulab.Identity.Contracts;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// The last step of every sign-in (F-5, F-11, F-20): reads the new session back from the Api and puts it in a
/// single-use ticket for <c>/account/sign-in-complete</c>, the only place that can write the cookie. One place for the
/// password, the code step, the Google step and the Google confirmation.
/// </summary>
public sealed class SignInHandOff(AuthClient auth, SignInTicketStore tickets)
{
    /// <summary>The address to navigate to with a full page load, or null when the Api did not confirm the session.</summary>
    public async Task<string?> CompletionPathAsync(TokenResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var session = await auth.GetSessionAsync(result.AccessToken!, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var lifetime = result.ExpiresInSeconds is > 0 ? TimeSpan.FromSeconds(result.ExpiresInSeconds.Value) : TokenLifetimes.AccessToken;
        var ticketId = tickets.Issue(new SignInTicket(
            session.Subject, session.Email, session.FullName, session.SessionJti, result.AccessToken!, result.RefreshToken!, lifetime, session.Permissions,
            session.PreferredLanguage));

        return $"/account/sign-in-complete?ticket={ticketId}";
    }
}
