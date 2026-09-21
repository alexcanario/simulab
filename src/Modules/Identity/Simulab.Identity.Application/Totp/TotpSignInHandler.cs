using Microsoft.AspNetCore.Identity;
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
    public async Task<Result<User>> CompleteAsync(string? challenge, string? code, CancellationToken cancellationToken = default)
    {
        var userId = string.IsNullOrWhiteSpace(challenge) ? null : await challenges.ConsumeAsync(challenge, cancellationToken);
        var user = userId is null ? null : await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.TwoFactorEnabled || user.Status != AccountStatus.Active)
        {
            return Result.Failure<User>(new Error(IdentityErrorCodes.TotpChallengeInvalid, ErrorKind.Validation));
        }

        var verified = await secondFactor.VerifyAsync(user, code);
        return verified.IsFailure ? Result.Failure<User>(verified.Error!) : Result.Success(user);
    }
}
