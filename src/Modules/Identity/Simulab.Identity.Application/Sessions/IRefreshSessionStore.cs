namespace Simulab.Identity.Application.Sessions;

/// <summary>
/// One row per signed-in session, keyed by the <c>session_jti</c> claim carried by both the access and
/// the refresh token of that session (BR4). Backed by Redis so a session survives a restart of the Api
/// and can be checked from any instance.
/// </summary>
public interface IRefreshSessionStore
{
    /// <summary>Starts a session. <paramref name="securityStamp"/> is the user's stamp now; a later reset or change renews it (F-7 BR9).</summary>
    Task CreateAsync(string sessionJti, Guid userId, string? securityStamp, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reads and removes the session (BR5): a refresh token is exchanged exactly once. Returns
    /// what the session remembered, or null when the token is unknown or was already used.
    /// </summary>
    Task<RefreshSession?> ConsumeAsync(string sessionJti, CancellationToken cancellationToken = default);

    /// <summary>Sign-out (BR6): drops the session without returning it. A missing session is not an error.</summary>
    Task RemoveAsync(string sessionJti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the session's access token unusable for the rest of its own lifetime (BR6, BR7).
    /// <paramref name="timeToLive"/> is the access token's remaining lifetime; the revocation entry expires
    /// with it, so Redis never keeps it longer than the token could have been valid anyway.
    /// </summary>
    Task RevokeAccessTokenAsync(string sessionJti, TimeSpan timeToLive, CancellationToken cancellationToken = default);

    Task<bool> IsAccessTokenRevokedAsync(string sessionJti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends every session of the user (F-7 BR9): each refresh session is removed and each session's access
    /// token revoked. <paramref name="exceptSessionJti"/> keeps the caller's own session (a password change).
    /// </summary>
    Task RevokeAllAsync(Guid userId, string? exceptSessionJti = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives a surviving session the user's new security stamp (F-7 BR9): after a password change the
    /// caller's own session keeps refreshing, while every other one fails the stamp check. A missing
    /// session is not an error.
    /// </summary>
    Task RestampAsync(string sessionJti, string? securityStamp, CancellationToken cancellationToken = default);
}
