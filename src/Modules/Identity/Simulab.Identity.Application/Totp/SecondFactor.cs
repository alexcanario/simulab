using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
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
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    private const int TotpDigits = 6;

    /// <summary>
    /// Success saves the accepted step and clears the failure count. A wrong code counts as a failed access, like a
    /// wrong password; while locked, even the right code is refused with the seconds left.
    /// </summary>
    // F-47 BR7, no transaction: a crash spends one of ten recovery codes or a TOTP step and nothing else; the other codes and the next TOTP code still work. A check's own writes must also survive a rollback (BR2).
    public async Task<Result<SecondFactorMethod>> VerifyAsync(User user, string? code, bool allowRecoveryCode = true)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<SecondFactorMethod>((await LockedAsync(user)).Error!);
        }

        if (await MatchesAsync(user, code, allowRecoveryCode) is { } method)
        {
            // The step (or the spent recovery code) is on the entity; ResetAccessFailedCountAsync skips the save when the count is already 0.
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.UpdateAsync(user);
            return Result.Success(method);
        }

        await userManager.AccessFailedAsync(user);
        if (!await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<SecondFactorMethod>(new Error(IdentityErrorCodes.TotpCodeInvalid, ErrorKind.BusinessRule));
        }

        // F-21 BR5: whoever asked for the code — the sign-in step or an account action — this failure
        // crossed the limit. The failed sign-in itself is recorded by the token endpoint (BR4).
        await accountEvents.AccountLockedAsync(user.Id);
        return Result.Failure<SecondFactorMethod>((await LockedAsync(user)).Error!);
    }

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds in <see cref="Error.Detail"/>, as the token endpoint does.</summary>
    public async Task<Result> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Which code matched, or null when none did.</summary>
    private async Task<SecondFactorMethod?> MatchesAsync(User user, string? code, bool allowRecoveryCode)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > AccountLimits.TotpCodeMaxLength || user.TotpSecretEncrypted is null)
        {
            return null;
        }

        if (trimmed.Length == TotpDigits && trimmed.All(char.IsAsciiDigit))
        {
            var step = MatchStep(user.TotpSecretEncrypted, trimmed);
            return step is not null && user.AcceptTotpStep(step.Value) ? SecondFactorMethod.TotpCode : null;
        }

        return allowRecoveryCode && await recoveryCodes.RedeemAsync(user, trimmed) ? SecondFactorMethod.RecoveryCode : null;
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
