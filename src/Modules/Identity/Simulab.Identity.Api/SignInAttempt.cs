using Microsoft.Extensions.Logging;
using Simulab.Identity.Application.Abstractions;

namespace Simulab.Identity.Api;

/// <summary>
/// The per-address sign-in limit of F-38 for one request: the account names that failed from one client
/// address (BR1, BR2). The limiter counts; this class names the address and writes the one warning (BR7).
/// </summary>
public sealed class SignInAttempt(ClientRateLimiter limiter, string key, string? address, ILogger<SignInAttempt> logger) : ISignInAttempt
{
    public TimeSpan RetryAfter { get; private set; }

    /// <summary>BR3: true, with <see cref="RetryAfter"/> set, while the address is at its limit. Reads nothing else.</summary>
    public bool IsAtLimit()
    {
        var left = limiter.SignInRetryAfter(key, IdentityRateLimits.SignInFailedAccountsPer15Minutes);
        RetryAfter = left ?? TimeSpan.Zero;
        return left is not null;
    }

    public bool TryCount(string accountName)
    {
        var reservation = limiter.ReserveSignInName(
            key,
            Normalize(accountName),
            IdentityRateLimits.SignInFailedAccountsPer15Minutes,
            IdentityRateLimits.SignInWindow);

        RetryAfter = reservation.RetryAfter;
        if (reservation.ReachedLimit)
        {
            // BR7: the address, never a user name, so the line can be acted on at the edge.
            logger.LogWarning("Sign-in failures reached the limit for client address {ClientAddress}", address);
        }

        return reservation.Allowed;
    }

    public void Clear(string accountName) => limiter.ReleaseSignInName(key, Normalize(accountName));

    /// <summary>BR1: the typed name, trimmed and lower-cased.</summary>
    private static string Normalize(string? accountName) => (accountName ?? string.Empty).Trim().ToLowerInvariant();
}
