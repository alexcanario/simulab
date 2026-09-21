namespace Simulab.Identity.Application.Totp;

/// <summary>
/// The single-use ticket between the password and the code at sign-in (F-11 BR9). Kept in Redis, like the
/// sessions, so it survives an Api restart and works on any instance.
/// </summary>
public interface ITotpChallengeStore
{
    /// <summary>Issues a challenge for <paramref name="userId"/>, valid until <paramref name="expiresAt"/>.</summary>
    Task<string> CreateAsync(Guid userId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and removes the challenge in one step: whatever happens next, it cannot be used again. Null when it
    /// is unknown, already used or past its expiry.
    /// </summary>
    Task<Guid?> ConsumeAsync(string challenge, CancellationToken cancellationToken = default);
}
