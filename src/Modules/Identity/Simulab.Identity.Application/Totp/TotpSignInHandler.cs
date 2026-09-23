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
    public async Task<Result<TotpSignIn>> CompleteAsync(string? challenge, string? code, CancellationToken cancellationToken = default)
    {
        var userId = string.IsNullOrWhiteSpace(challenge) ? null : await challenges.ConsumeAsync(challenge, cancellationToken);
        var user = userId is null ? null : await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.TwoFactorEnabled || user.Status != AccountStatus.Active)
        {
            // F-21 BR4: a challenge that is spent, expired or no longer belongs to a two-factor account.
            await accountEvents.SignInFailedAsync(userId, AccountEventReason.ChallengeInvalid, cancellationToken);
            return Result.Failure<TotpSignIn>(new Error(IdentityErrorCodes.TotpChallengeInvalid, ErrorKind.Validation));
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

        // F-21 BR3: this step issues the tokens, so the sign-in is this account's, by the code it used.
        var method = verified.Value == SecondFactorMethod.RecoveryCode
            ? AccountEventMethod.RecoveryCode
            : AccountEventMethod.TotpCode;
        await accountEvents.SignInSucceededAsync(user.Id, method, cancellationToken);

        return Result.Success(new TotpSignIn(user, verified.Value));
    }
}
