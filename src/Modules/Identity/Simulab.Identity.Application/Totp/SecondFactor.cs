using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Totp;

/// <summary>
/// Checks a code for an account that has a secret (F-11): at sign-in, to regenerate the recovery codes and to turn
/// two-factor off. One place for the lockout (BR10), the replay rule (BR4) and the recovery codes (BR6).
/// </summary>
public sealed class SecondFactor(
    UserManager<User> userManager,
    ITotpAuthenticator authenticator,
    ITotpSecretProtector protector,
    RecoveryCodes recoveryCodes,
    TimeProvider timeProvider)
{
    private const int TotpDigits = 6;

    /// <summary>
    /// Success saves the accepted step and clears the failure count. A wrong code counts as a failed access, like a
    /// wrong password; while locked, even the right code is refused with the seconds left.
    /// </summary>
    public async Task<Result> VerifyAsync(User user, string? code, bool allowRecoveryCode = true)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (await userManager.IsLockedOutAsync(user))
        {
            return await LockedAsync(user);
        }

        if (await MatchesAsync(user, code, allowRecoveryCode))
        {
            // The step (or the spent recovery code) is on the entity; ResetAccessFailedCountAsync skips the save when the count is already 0.
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.UpdateAsync(user);
            return Result.Success();
        }

        await userManager.AccessFailedAsync(user);
        return await userManager.IsLockedOutAsync(user)
            ? await LockedAsync(user)
            : Result.Failure(new Error(IdentityErrorCodes.TotpCodeInvalid, ErrorKind.BusinessRule));
    }

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds in <see cref="Error.Detail"/>, as the token endpoint does.</summary>
    public async Task<Result> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    private async Task<bool> MatchesAsync(User user, string? code, bool allowRecoveryCode)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > AccountLimits.TotpCodeMaxLength || user.TotpSecretEncrypted is null)
        {
            return false;
        }

        if (trimmed.Length == TotpDigits && trimmed.All(char.IsAsciiDigit))
        {
            var step = MatchStep(user.TotpSecretEncrypted, trimmed);
            return step is not null && user.AcceptTotpStep(step.Value);
        }

        return allowRecoveryCode && await recoveryCodes.RedeemAsync(user, trimmed);
    }

    /// <summary>A secret written with another key reads as "no match", never as a server error.</summary>
    private long? MatchStep(string protectedSecret, string code)
    {
        string secret;
        try
        {
            secret = protector.Unprotect(protectedSecret);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }

        return authenticator.MatchStep(secret, code, timeProvider.GetUtcNow());
    }
}
