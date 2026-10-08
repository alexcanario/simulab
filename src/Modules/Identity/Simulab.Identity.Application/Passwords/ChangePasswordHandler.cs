using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// A signed-in user changes the password (F-7 UC6). Imported from Simulae's <c>ChangePasswordCommandHandler</c>:
/// the same lockout as sign-in (BR8), and every other session ends while the caller's stays (BR9).
/// </summary>
public sealed class ChangePasswordHandler(
    UserManager<User> userManager,
    IRefreshSessionStore sessions,
    IPasswordMailer mailer,
    IIdentityUnitOfWork unitOfWork,
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(
        Guid userId,
        string? callerSessionJti,
        string? currentPassword,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Failure(IdentityErrorCodes.PasswordChangeCurrentInvalid, ErrorKind.BusinessRule);
        }

        // F-20 BR11: an account created with Google has no current password to prove; it sets one through the reset.
        if (!await userManager.HasPasswordAsync(user))
        {
            return Failure(IdentityErrorCodes.PasswordNotSet, ErrorKind.BusinessRule);
        }

        // BR8: while locked, even the right current password is refused, as on sign-in.
        if (await userManager.IsLockedOutAsync(user))
        {
            return await LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            if (!await userManager.IsLockedOutAsync(user))
            {
                return Failure(IdentityErrorCodes.PasswordChangeCurrentInvalid, ErrorKind.BusinessRule);
            }

            // F-21 BR5: the attempt itself is not a recorded event, but the lockout it just caused is.
            await accountEvents.AccountLockedAsync(user.Id, cancellationToken);
            return await LockedAsync(user);
        }

        var refusal = await PasswordRules.CheckNewPasswordAsync(
            userManager, user, newPassword, IdentityErrorCodes.PasswordChangeTooWeak, IdentityErrorCodes.PasswordChangeSameAsCurrent);
        if (refusal is not null)
        {
            return Result.Failure(refusal);
        }

        // F-47 BR1, BR2: every write below is one transaction, opened after every check; a failure before the
        // commit leaves the old password in place and no notice behind.
        await using (var transaction = await unitOfWork.BeginAsync(cancellationToken))
        {
            // F-53 BR8: any successful password set clears the mark; it travels in the same UPDATE as the password.
            user.ClearPasswordChangeRequirement();
            var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword!);
            if (!changed.Succeeded)
            {
                return Failure(IdentityErrorCodes.PasswordChangeTooWeak, ErrorKind.Validation);
            }

            // BR10.
            await userManager.ResetAccessFailedCountAsync(user);

            // F-47 BR3: the notice is staged and saved before the Redis calls, so a failed commit never leaves
            // a session alive that the change was meant to end.
            await PasswordNotice.EnqueueAsync(mailer, unitOfWork, user, timeProvider.GetUtcNow(), cancellationToken);

            // BR9: the other devices sign out; this one keeps working, with the stamp the change just renewed.
            await sessions.RevokeAllAsync(user.Id, callerSessionJti, cancellationToken);
            if (callerSessionJti is not null)
            {
                await sessions.RestampAsync(callerSessionJti, user.SecurityStamp, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        // F-21 BR1, BR7 and F-47 BR4: after the commit, once the new password is really in place.
        await accountEvents.RecordAsync(user.Id, AccountEventType.PasswordChanged, cancellationToken);
        return Result.Success();
    }

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds in <see cref="Error.Detail"/>, as the token endpoint does.</summary>
    private async Task<Result> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
