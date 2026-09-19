namespace Simulab.Identity.Application.Sessions;

/// <summary>
/// What a refresh session remembers (F-5 BR4): whose it is, and the user's security stamp when it began.
/// A password reset or change renews the stamp, so a session from before it can never refresh again, even
/// one that slipped past <see cref="IRefreshSessionStore.RevokeAllAsync"/> in a race (F-7 BR9).
/// </summary>
public sealed record RefreshSession(Guid UserId, string? SecurityStamp);
