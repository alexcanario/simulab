using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Verification;

/// <summary>What the resend did. The endpoint decides what the caller is told (BR11).</summary>
public enum ResendOutcome
{
    /// <summary>The request was accepted. It does not say whether an email was sent.</summary>
    Accepted,

    /// <summary>The per-address limits refused it and nothing was sent (BR12).</summary>
    Throttled
}

/// <summary>
/// Sends a new verification link (UC3). Nothing in the outcome depends on whether the address exists,
/// except the throttle, which only a real pending account can reach.
/// </summary>
public sealed class ResendVerificationHandler(
    IUserDirectory userDirectory,
    IEmailVerificationTokenStore tokenStore,
    IVerificationMailer mailer,
    TimeProvider timeProvider)
{
    public async Task<ResendOutcome> HandleAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ResendOutcome.Accepted;
        }

        var user = await userDirectory.FindByEmailIgnoringTenantAsync(email, cancellationToken);

        // BR11: an unknown address and an account that is already active both end here, having done nothing.
        if (user is null || user.Status != AccountStatus.Pending)
        {
            return ResendOutcome.Accepted;
        }

        var now = timeProvider.GetUtcNow();
        if (await IsThrottledAsync(user.Id, now, cancellationToken))
        {
            return ResendOutcome.Throttled;
        }

        // The older links stop working before the new one exists, so only one link is ever valid.
        await tokenStore.ConsumePendingForUserAsync(user.Id, now, cancellationToken);

        var (rawToken, tokenHash) = SecureToken.Generate();

        // F-13 BR2: staged first, written by the store's save, in the token's own transaction.
        await mailer.SendAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);

        await tokenStore.AddAsync(
            new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.Add(VerificationTokenPolicy.Lifetime)
            },
            cancellationToken);

        return ResendOutcome.Accepted;
    }

    /// <summary>
    /// BR12, per address: one email a minute and five an hour. The tokens already record when each email
    /// went out, so the limit needs no extra state and survives a restart.
    /// </summary>
    private async Task<bool> IsThrottledAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lastCreatedAt = await tokenStore.LastCreatedAtAsync(userId, cancellationToken);
        if (lastCreatedAt is not null && now - lastCreatedAt.Value < VerificationTokenPolicy.ResendCooldown)
        {
            return true;
        }

        var recent = await tokenStore.CountCreatedSinceAsync(userId, now - VerificationTokenPolicy.ResendWindow, cancellationToken);
        return recent >= VerificationTokenPolicy.MaxResendsPerWindow;
    }
}
