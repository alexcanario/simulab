using System.Security.Claims;
using Simulab.Identity.Contracts;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// The only way Web code gets an access token (B-3, BR2, BR6). It refreshes the token before it expires,
/// re-reads the permissions on each refresh (F-6 BR5), and ends the web session when the Api refuses it
/// (BR3). An Api that does not answer never ends a session: only an explicit refusal does.
/// </summary>
public sealed class WebSessionTokenAccessor(
    IWebSessionStore store,
    AuthClient auth,
    SessionRefreshGate gate,
    TimeProvider timeProvider)
{
    /// <summary>How often a session is confirmed with the Api outside page loads, and inside an open page (BR4, BR5).</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    /// <summary>A token this close to its expiry is refreshed first, so a call never leaves with one that dies on the way.</summary>
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    /// <summary>How long a refresh may take once started, whoever asked for it.</summary>
    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(30);

    public async Task<string?> GetAccessTokenAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var webSessionId = user.FindFirstValue(WebAuthClaims.WebSessionId);
        return webSessionId is null ? null : (await GetFreshAsync(webSessionId, cancellationToken))?.AccessToken;
    }

    /// <summary>The session with an access token valid for at least a minute; null when the session ended.</summary>
    public async Task<WebSession?> GetFreshAsync(string webSessionId, CancellationToken cancellationToken = default)
    {
        var session = await store.GetAsync(webSessionId, cancellationToken);
        if (session is null || !NeedsRefresh(session))
        {
            return session;
        }

        using (await gate.EnterAsync(webSessionId, cancellationToken))
        {
            // Whoever held the gate before may have refreshed already: its result is the one to use.
            session = await store.GetAsync(webSessionId, cancellationToken);
            if (session is null || !NeedsRefresh(session))
            {
                return session;
            }

            // Past this point the Api consumes the single-use refresh token (F-5 BR5). A caller that goes
            // away (a closed tab, an aborted asset request) must not lose the new pair: finish and save it
            // regardless, with a bound of its own.
            using var refreshTimeout = new CancellationTokenSource(RefreshTimeout, timeProvider);
            var result = await auth.RefreshAsync(session.RefreshToken, refreshTimeout.Token);
            if (!result.IsSuccess)
            {
                if (!result.IsRejected)
                {
                    return session;
                }

                await store.RemoveAsync(webSessionId, cancellationToken);
                return null;
            }

            var now = timeProvider.GetUtcNow();
            var lookup = await auth.LookUpSessionAsync(result.AccessToken!, refreshTimeout.Token);
            var refreshed = new WebSession(
                lookup.Session?.SessionJti ?? session.ApiSessionJti,
                result.AccessToken!,
                result.RefreshToken!,
                now.Add(ExpiresIn(result.ExpiresInSeconds)),
                lookup.Session?.Permissions ?? session.Permissions,
                lookup.Status == SessionLookupStatus.Alive ? now : session.CheckedAt,
                session.ExpiresAt);

            await store.SaveAsync(webSessionId, refreshed, CancellationToken.None);
            return refreshed;
        }
    }

    /// <summary>
    /// Confirms the session with the Api (BR3-BR5) and returns it, or null when it ended. Without
    /// <paramref name="force"/>, a session confirmed in the last <see cref="CheckInterval"/> is not asked again.
    /// </summary>
    public async Task<WebSession?> CheckAsync(string webSessionId, bool force, CancellationToken cancellationToken = default)
    {
        var session = await GetFreshAsync(webSessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (!force && now - session.CheckedAt < CheckInterval)
        {
            return session;
        }

        // The refresh above did not get an answer, so the token is still the expired one. The Api would
        // answer 401 for the expiry alone, which is not a refusal of the session (BR3): ask next time.
        if (session.AccessTokenExpiresAt <= now)
        {
            return session;
        }

        var lookup = await auth.LookUpSessionAsync(session.AccessToken, cancellationToken);
        if (lookup.Status == SessionLookupStatus.Unavailable)
        {
            return session;
        }

        using (await gate.EnterAsync(webSessionId, cancellationToken))
        {
            if (lookup.Status == SessionLookupStatus.Ended)
            {
                await store.RemoveAsync(webSessionId, cancellationToken);
                return null;
            }

            var confirmed = session with { Permissions = lookup.Session!.Permissions, CheckedAt = now };

            // A refresh that finished meanwhile wrote newer tokens: keep them rather than the ones read above.
            var current = await store.GetAsync(webSessionId, cancellationToken);
            if (current is not null && current.AccessToken == session.AccessToken)
            {
                await store.SaveAsync(webSessionId, confirmed, cancellationToken);
            }

            return confirmed;
        }
    }

    /// <summary>Sign-out (F-5 BR6): the entry goes, whatever the Api said.</summary>
    public Task EndAsync(string webSessionId, CancellationToken cancellationToken = default) =>
        store.RemoveAsync(webSessionId, cancellationToken);

    private bool NeedsRefresh(WebSession session) => session.AccessTokenExpiresAt - timeProvider.GetUtcNow() < RefreshMargin;

    private static TimeSpan ExpiresIn(int? seconds) => seconds is > 0 ? TimeSpan.FromSeconds(seconds.Value) : TokenLifetimes.AccessToken;
}
