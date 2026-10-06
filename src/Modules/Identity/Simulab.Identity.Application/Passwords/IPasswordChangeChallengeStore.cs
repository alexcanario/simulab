using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// The single-use ticket between a right password and the new one (F-53 BR6). A kind of its own: it never
/// shares a key space with the two-factor challenge, so neither can stand in for the other.
/// </summary>
public interface IPasswordChangeChallengeStore
{
    /// <summary>Issues a challenge for <paramref name="userId"/>, valid until <paramref name="expiresAt"/>.</summary>
    Task<string> CreateAsync(
        Guid userId,
        string? securityStamp,
        AccountEventMethod method,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the challenge without spending it. Null when it is unknown, already used or past its expiry.</summary>
    Task<PasswordChangeChallenge?> PeekAsync(string challenge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the challenge in one atomic step. True for exactly one of several concurrent callers; false when it
    /// is unknown, already used or past its expiry.
    /// </summary>
    Task<bool> TrySpendAsync(string challenge, CancellationToken cancellationToken = default);
}
