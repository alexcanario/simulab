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
    public async Task<bool> IsAtLimitAsync()
    {
        var check = await limiter.CheckSignInAsync(key, IdentityRateLimits.SignInFailedAccountsPer15Minutes);
        RetryAfter = check.RetryAfter;
        ReportFirstRefusal(check);
        return !check.Allowed;
    }

    public async Task<bool> TryCountAsync(string accountName)
    {
        var reservation = await limiter.ReserveSignInNameAsync(
            key,
            Normalize(accountName),
            IdentityRateLimits.SignInFailedAccountsPer15Minutes,
            IdentityRateLimits.SignInWindow);

        RetryAfter = reservation.RetryAfter;
        ReportFirstRefusal(reservation);
        return reservation.Allowed;
    }

    /// <summary>BR7: the first refusal of the window writes the address, never a user name, so the line can be acted on at the edge.</summary>
    private void ReportFirstRefusal(SignInReservation decision)
    {
        if (decision.FirstRefusal)
        {
            logger.LogWarning("Sign-in refused: the failure limit was reached for client address {ClientAddress}", address);
        }
    }

    public Task ClearAsync(string accountName) => limiter.ReleaseSignInNameAsync(key, Normalize(accountName));

    /// <summary>BR1: the typed name, trimmed and lower-cased.</summary>
    private static string Normalize(string? accountName) => (accountName ?? string.Empty).Trim().ToLowerInvariant();
}
