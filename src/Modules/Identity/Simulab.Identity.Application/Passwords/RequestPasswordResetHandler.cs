using Microsoft.Extensions.Logging;
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
    TimeProvider timeProvider,
    ILogger<RequestPasswordResetHandler> logger)
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
        await tokenStore.AddAsync(
            new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.Add(PasswordResetPolicy.Lifetime)
            },
            cancellationToken);

        // BR1: a mail server that fails must not turn into a 500 that only real accounts can reach.
        try
        {
            await mailer.SendResetLinkAsync(user.Email!, rawToken, user.PreferredLanguage, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sending a password reset email failed.");
        }
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
