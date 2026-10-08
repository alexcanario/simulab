using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Security;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// The sign-in step of an account marked "must change password" (F-53 UC2): the password (and code) step hands out a
/// challenge instead of tokens, and this step trades the challenge and a new password for the account.
/// </summary>
public sealed class ForcedPasswordChangeHandler(
    UserManager<User> userManager,
    IPasswordChangeChallengeStore challenges,
    IRefreshSessionStore sessions,
    IIdentityUnitOfWork unitOfWork,
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    /// <summary>BR3: long enough to think of a password, short enough not to leave a half-open door.</summary>
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>After the last factor of the sign-in: a single-use challenge for the change step. <paramref name="method"/> is that factor (BR9).</summary>
    public Task<string> IssueChallengeAsync(User user, AccountEventMethod method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        return challenges.CreateAsync(user.Id, user.SecurityStamp, method, timeProvider.GetUtcNow().Add(ChallengeLifetime), cancellationToken);
    }

    /// <summary>
    /// BR6, BR7: the challenge is read, checked against the account, the new password is checked, and only then is it
    /// spent, before any write. A refused password keeps the challenge; every other refusal ends it.
    /// </summary>
    public async Task<Result<User>> CompleteAsync(
        string? challenge,
        string? newPassword,
        ISignInAttempt? attempt = null,
        CancellationToken cancellationToken = default)
    {
        var issued = string.IsNullOrWhiteSpace(challenge) ? null : await challenges.PeekAsync(challenge, cancellationToken);
        var user = issued is null ? null : await userManager.FindByIdAsync(issued.UserId.ToString());
        if (issued is null
            || user is null
            || user.Status != AccountStatus.Active
            || !user.MustChangePassword
            || !string.Equals(issued.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            return await InvalidAsync(issued?.UserId, cancellationToken);
        }

        // F-38 BR1, BR3: the account is known only now, so it is counted here, before anything is checked or
        // written for it; at the limit the step ends with no event and the challenge untouched.
        var accountName = user.UserName ?? user.Email ?? string.Empty;
        if (attempt is not null && !await attempt.TryCountAsync(accountName))
        {
            return Result.Failure<User>(new Error(
                IdentityErrorCodes.SignInRateLimited,
                ErrorKind.BusinessRule,
                Math.Max(0, (int)Math.Ceiling(attempt.RetryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture)));
        }

        // BR5, BR6: a refused password changes nothing and leaves the challenge usable.
        var refusal = await PasswordRules.CheckNewPasswordAsync(
            userManager, user, newPassword, IdentityErrorCodes.PasswordChangeTooWeak, IdentityErrorCodes.PasswordChangeSameAsCurrent);
        if (refusal is not null)
        {
            return Result.Failure<User>(refusal);
        }

        // BR6: spent before any write, atomically; the request that loses the race is refused like any other.
        if (!await challenges.TrySpendAsync(challenge!, cancellationToken))
        {
            return await InvalidAsync(user.Id, cancellationToken);
        }

        // F-47 BR1, BR2: every write below is one transaction, opened after every check.
        await using (var transaction = await unitOfWork.BeginAsync(cancellationToken))
        {
            // BR4, BR8: the mark is on the entity before the reset, which saves it with the password in one UPDATE.
            user.ClearPasswordChangeRequirement();

            // The rules were checked above, so a failure here is not the user's password: a server error.
            var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
            (await userManager.ResetPasswordAsync(user, identityToken, newPassword!)).ThrowIfFailed("Setting the new password");

            await userManager.ResetAccessFailedCountAsync(user);

            // BR4: any session the old password opened ends; the new one is issued by the token endpoint.
            await sessions.RevokeAllAsync(user.Id, exceptSessionJti: null, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        // BR9: after the commit; one sign-in event, with the last factor the user proved. No notice email.
        await accountEvents.RecordAsync(user.Id, AccountEventType.PasswordChanged, cancellationToken);
        await accountEvents.SignInSucceededAsync(user.Id, issued.Method, cancellationToken);
        if (attempt is not null)
        {
            await attempt.ClearAsync(accountName);
        }

        return Result.Success(user);
    }

    private async Task<Result<User>> InvalidAsync(Guid? userId, CancellationToken cancellationToken)
    {
        // F-21 BR4: a challenge that is spent, expired, of the other kind or no longer fits its account.
        await accountEvents.SignInFailedAsync(userId, AccountEventReason.ChallengeInvalid, cancellationToken);
        return Result.Failure<User>(new Error(IdentityErrorCodes.PasswordChangeChallengeInvalid, ErrorKind.Validation));
    }
}
