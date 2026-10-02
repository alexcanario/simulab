using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Verification;

/// <summary>Consumes a verification token and activates the account (UC2).</summary>
public sealed class VerifyEmailHandler(
    UserManager<User> userManager,
    IEmailVerificationTokenStore tokenStore,
    TimeProvider timeProvider)
{
    // F-47 BR7, no transaction: a crash leaves an active account with an unconsumed link, and F-4 BR10 already answers "verified" for any link of an active account.
    public async Task<VerificationOutcome> HandleAsync(string? rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return VerificationOutcome.Invalid;
        }

        var token = await tokenStore.FindByHashAsync(SecureToken.Hash(rawToken), cancellationToken);
        if (token is null)
        {
            return VerificationOutcome.Invalid;
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
        {
            return VerificationOutcome.Invalid;
        }

        // BR10: the link in an inbox is clicked more than once. Once the account is active the answer is
        // success, whatever state the token is in, so the second click does not look like a failure.
        if (user.Status == AccountStatus.Active)
        {
            return VerificationOutcome.Verified;
        }

        if (token.IsConsumed)
        {
            return VerificationOutcome.Invalid;
        }

        var now = timeProvider.GetUtcNow();
        if (token.IsExpiredAt(now))
        {
            return VerificationOutcome.Expired;
        }

        user.VerifyEmail(now);
        await userManager.UpdateAsync(user);
        await tokenStore.ConsumeAsync(token, now, cancellationToken);

        return VerificationOutcome.Verified;
    }
}
