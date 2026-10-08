using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Totp;

/// <summary>
/// The two steps of a sign-in with two-factor on (F-11 UC3, UC4, BR9): the password step issues a challenge
/// instead of tokens, and the code step trades the challenge and a code for the account.
/// </summary>
public sealed class TotpSignInHandler(
    UserManager<User> userManager,
    ITotpChallengeStore challenges,
    SecondFactor secondFactor,
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    /// <summary>BR9: long enough to pick up the phone, short enough not to leave a half-open door.</summary>
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>After the right password: a single-use challenge for the code step.</summary>
    public Task<string> IssueChallengeAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        return challenges.CreateAsync(user.Id, timeProvider.GetUtcNow().Add(ChallengeLifetime), cancellationToken);
    }

    /// <summary>
    /// The challenge is spent before the code is checked, so a wrong code costs the whole attempt (BR9). A
    /// challenge whose account no longer has two-factor on reads as invalid too.
    /// </summary>
    // F-47 BR7, no transaction: as SecondFactor.VerifyAsync, a crash leaves a spent code and no tokens; the visitor signs in again.
    public async Task<Result<TotpSignIn>> CompleteAsync(string? challenge, string? code, ISignInAttempt? attempt = null, CancellationToken cancellationToken = default)
    {
        var userId = string.IsNullOrWhiteSpace(challenge) ? null : await challenges.ConsumeAsync(challenge, cancellationToken);
        var user = userId is null ? null : await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.TwoFactorEnabled || user.Status != AccountStatus.Active)
        {
            // F-21 BR4: a challenge that is spent, expired or no longer belongs to a two-factor account.
            await accountEvents.SignInFailedAsync(userId, AccountEventReason.ChallengeInvalid, cancellationToken);
            return Result.Failure<TotpSignIn>(new Error(IdentityErrorCodes.TotpChallengeInvalid, ErrorKind.Validation));
        }

        // F-38 BR1, BR3: the account is known only now, so it is counted here, before anything is checked or
        // written for it; at the limit the step ends with no failure count and no account event.
        var accountName = user.UserName ?? user.Email ?? string.Empty;
        if (attempt is not null && !await attempt.TryCountAsync(accountName))
        {
            return Result.Failure<TotpSignIn>(new Error(
                IdentityErrorCodes.SignInRateLimited,
                ErrorKind.BusinessRule,
                Math.Max(0, (int)Math.Ceiling(attempt.RetryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var verified = await secondFactor.VerifyAsync(user, code);
        if (verified.IsFailure)
        {
            // F-21 BR4. The lockout this failure may have caused is recorded by the check itself (BR5).
            var reason = verified.Error!.Code == IdentityErrorCodes.AccountLocked
                ? AccountEventReason.LockedOut
                : AccountEventReason.WrongCode;
            await accountEvents.SignInFailedAsync(user.Id, reason, cancellationToken);
            return Result.Failure<TotpSignIn>(verified.Error!);
        }

        // F-53 BR3: a marked account gets a password-change challenge instead of tokens, so this step is not the
        // sign-in: no event and the name stays in the set. The change step records the sign-in and clears it.
        if (user.MustChangePassword)
        {
            return Result.Success(new TotpSignIn(user, verified.Value));
        }

        // F-21 BR3: this step issues the tokens, so the sign-in is this account's, by the code it used.
        await accountEvents.SignInSucceededAsync(user.Id, MethodOf(verified.Value), cancellationToken);
        if (attempt is not null)
        {
            await attempt.ClearAsync(accountName);
        }

        return Result.Success(new TotpSignIn(user, verified.Value));
    }

    /// <summary>The account event method of the code a step accepted (F-21 BR3, F-53 BR9).</summary>
    public static AccountEventMethod MethodOf(SecondFactorMethod method) =>
        method == SecondFactorMethod.RecoveryCode ? AccountEventMethod.RecoveryCode : AccountEventMethod.TotpCode;
}
