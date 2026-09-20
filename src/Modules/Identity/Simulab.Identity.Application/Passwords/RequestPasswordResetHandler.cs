using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// Sends a reset link (F-7 UC1). The caller always gets the same answer (BR1): nothing it can observe
/// depends on whether the address exists, is pending, or was just sent a link.
/// </summary>
public sealed class RequestPasswordResetHandler(
    IUserDirectory userDirectory,
    IPasswordResetTokenStore tokenStore,
    IPasswordMailer mailer,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await userDirectory.FindByEmailIgnoringTenantAsync(email.Trim(), cancellationToken);

        // BR1: Pending and Active get a link (BR6: using it activates a pending account); nothing else does.
        if (user is null || user.Status is not (AccountStatus.Pending or AccountStatus.Active))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        if (await IsThrottledAsync(user.Id, now, cancellationToken))
        {
            return;
        }

        // The older links stop working before the new one exists, so only one link is ever valid.
        await tokenStore.ConsumePendingForUserAsync(user.Id, now, cancellationToken);

        var (rawToken, tokenHash) = SecureToken.Generate();

        // F-13 BR2, BR3: the mailer stages the job and the store's save writes both rows. Nothing here
        // talks to the mail server any more, so BR1 holds by construction: a slow or broken SMTP server
        // cannot make this path answer differently, or later, than the unknown-address path above.
        await mailer.SendResetLinkAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);

        await tokenStore.AddAsync(
            new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.Add(PasswordResetPolicy.Lifetime)
            },
            cancellationToken);
    }

    /// <summary>BR3, per account and silent: one email a minute, five an hour, counted from the token rows.</summary>
    private async Task<bool> IsThrottledAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lastCreatedAt = await tokenStore.LastCreatedAtAsync(userId, cancellationToken);
        if (lastCreatedAt is not null && now - lastCreatedAt.Value < PasswordResetPolicy.Cooldown)
        {
            return true;
        }

        var recent = await tokenStore.CountCreatedSinceAsync(userId, now - PasswordResetPolicy.Window, cancellationToken);
        return recent >= PasswordResetPolicy.MaxEmailsPerWindow;
    }
}
