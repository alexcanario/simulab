using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// Sets a new password with the token from the reset email (F-7 UC2-UC5). Imported from Simulae's
/// <c>ResetPasswordCommandHandler</c>, which cleared the failed count but left an active lockout, and left a
/// pending account pending; both are fixed here (BR5, BR6).
/// </summary>
public sealed class ResetPasswordHandler(
    UserManager<User> userManager,
    IPasswordResetTokenStore tokenStore,
    IEmailVerificationTokenStore verificationTokens,
    IRefreshSessionStore sessions,
    IPasswordMailer mailer,
    TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(string? rawToken, string? newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Failure(IdentityErrorCodes.PasswordResetInvalid, ErrorKind.Validation);
        }

        var token = await tokenStore.FindByHashAsync(SecureToken.Hash(rawToken), cancellationToken);
        if (token is null || token.IsConsumed)
        {
            return Failure(IdentityErrorCodes.PasswordResetInvalid, ErrorKind.Validation);
        }

        var now = timeProvider.GetUtcNow();
        if (token.IsExpiredAt(now))
        {
            return Failure(IdentityErrorCodes.PasswordResetExpired, ErrorKind.BusinessRule);
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
        {
            return Failure(IdentityErrorCodes.PasswordResetInvalid, ErrorKind.Validation);
        }

        // BR7: a refused password changes nothing and leaves the link usable.
        var refusal = await PasswordRules.CheckNewPasswordAsync(
            userManager, user, newPassword, IdentityErrorCodes.PasswordResetTooWeak, IdentityErrorCodes.PasswordResetSameAsCurrent);
        if (refusal is not null)
        {
            return Result.Failure(refusal);
        }

        // Our own token proved the mailbox; Identity's own reset token is only the key its API asks for.
        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, identityToken, newPassword!);
        if (!reset.Succeeded)
        {
            return Failure(IdentityErrorCodes.PasswordResetTooWeak, ErrorKind.Validation);
        }

        await tokenStore.ConsumeAsync(token, now, cancellationToken);

        // BR5: the person who opened the link owns the mailbox; making them wait out a lockout protects nothing.
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);

        // BR6: the link proved the mailbox exactly as the verification link does.
        if (user.Status == AccountStatus.Pending)
        {
            user.VerifyEmail(now);
            await userManager.UpdateAsync(user);
            await verificationTokens.ConsumePendingForUserAsync(user.Id, now, cancellationToken);
        }

        // BR9: whoever holds the old password may hold a session too.
        await sessions.RevokeAllAsync(user.Id, exceptSessionJti: null, cancellationToken);

        await mailer.SendPasswordChangedAsync(user.Email!, now, user.PreferredLanguage, cancellationToken);
        return Result.Success();
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
