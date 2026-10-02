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
    IIdentityUnitOfWork unitOfWork,
    IAccountEventLog accountEvents,
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

        // F-47 BR1, BR2: every write below is one transaction, opened after every check. Disposed before the
        // commit it rolls back, so the link is not spent and the account is as it was.
        await using (var transaction = await unitOfWork.BeginAsync(cancellationToken))
        {
            // BR2: the link is spent before anything changes, in one conditional update, so two concurrent
            // resets with the same link cannot both succeed.
            if (!await tokenStore.TryConsumeAsync(token, now, cancellationToken))
            {
                return Failure(IdentityErrorCodes.PasswordResetInvalid, ErrorKind.Validation);
            }

            // Our own token proved the mailbox; Identity's own reset token is only the key its API asks for.
            // The rules were checked above, so a failure here is not the visitor's password: a server error.
            var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, identityToken, newPassword!);
            if (!reset.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Resetting the password failed after the checks passed: {string.Join(", ", reset.Errors.Select(error => error.Code))}.");
            }

            // BR5: the person who opened the link owns the mailbox; making them wait out a lockout protects nothing.
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);

            // BR6: the link proved the mailbox exactly as the verification link does.
            if (user.Status == AccountStatus.Pending)
            {
                user.VerifyEmail(now);
                (await userManager.UpdateAsync(user)).ThrowIfFailed("Activating the account");
                await verificationTokens.ConsumePendingForUserAsync(user.Id, now, cancellationToken);
            }

            // F-47 BR3: the notice is staged and saved before the Redis call, so a failed commit never leaves
            // a session alive that the reset was meant to end.
            await PasswordNotice.EnqueueAsync(mailer, unitOfWork, user, now, cancellationToken);

            // BR9: whoever holds the old password may hold a session too.
            await sessions.RevokeAllAsync(user.Id, exceptSessionJti: null, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        // F-21 BR1, BR7 and F-47 BR4: after the commit, once the password is really set.
        await accountEvents.RecordAsync(user.Id, AccountEventType.PasswordResetCompleted, cancellationToken);
        return Result.Success();
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
