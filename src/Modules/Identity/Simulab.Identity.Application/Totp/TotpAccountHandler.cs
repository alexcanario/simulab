using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Totp;

/// <summary>
/// Two-factor on the caller's own account (F-11 UC1, UC2, UC5, UC6; BR1): the Security page's status, enrolment,
/// confirmation, new recovery codes and turning it off. The user is always the token subject.
/// </summary>
public sealed class TotpAccountHandler(
    UserManager<User> userManager,
    ITotpAuthenticator authenticator,
    ITotpSecretProtector protector,
    RecoveryCodes recoveryCodes,
    SecondFactor secondFactor,
    IAccountEventLog accountEvents,
    TimeProvider timeProvider)
{
    public async Task<TotpStatusResponse?> GetStatusAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        return user.TwoFactorEnabled
            ? new TotpStatusResponse(true, user.TotpEnabledAt, await recoveryCodes.CountAsync(user))
            : new TotpStatusResponse(false);
    }

    /// <summary>BR2: a new secret, encrypted on the account; two-factor stays off until <see cref="ConfirmAsync"/>.</summary>
    public async Task<Result<TotpEnrolmentResponse>> StartAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<TotpEnrolmentResponse>(new Error(IdentityErrorCodes.UserNotFound, ErrorKind.NotFound));
        }

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<TotpEnrolmentResponse>(new Error(IdentityErrorCodes.TotpAlreadyEnabled, ErrorKind.Conflict));
        }

        var secret = authenticator.GenerateSecret();
        user.StartTotpEnrolment(protector.Protect(secret));
        await userManager.UpdateAsync(user);

        var uri = authenticator.OtpAuthUri(user.Email!, secret);
        return Result.Success(new TotpEnrolmentResponse(secret, uri, authenticator.QrCodeDataUri(uri)));
    }

    /// <summary>
    /// BR2, BR5: the first valid code turns two-factor on and returns the ten recovery codes. Only a code from the
    /// app counts here: there are no recovery codes yet.
    /// </summary>
    public async Task<Result<RecoveryCodesResponse>> ConfirmAsync(Guid userId, string? code)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<RecoveryCodesResponse>(new Error(IdentityErrorCodes.UserNotFound, ErrorKind.NotFound));
        }

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<RecoveryCodesResponse>(new Error(IdentityErrorCodes.TotpAlreadyEnabled, ErrorKind.Conflict));
        }

        if (user.TotpSecretEncrypted is null)
        {
            return Result.Failure<RecoveryCodesResponse>(new Error(IdentityErrorCodes.TotpNotEnabled, ErrorKind.Conflict));
        }

        var verified = await secondFactor.VerifyAsync(user, code, allowRecoveryCode: false);
        if (verified.IsFailure)
        {
            return Result.Failure<RecoveryCodesResponse>(verified.Error!);
        }

        user.EnableTotp(timeProvider.GetUtcNow());
        await userManager.UpdateAsync(user);

        var codes = new RecoveryCodesResponse(await recoveryCodes.ReplaceAsync(user));

        // F-21 BR1: two-factor is on from here.
        await accountEvents.RecordAsync(user.Id, AccountEventType.TwoFactorEnabled);
        return Result.Success(codes);
    }

    /// <summary>BR7: a valid code or recovery code, then ten new codes; the previous ones stop working at once.</summary>
    public async Task<Result<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync(Guid userId, string? code)
    {
        var user = await EnabledUserAsync(userId);
        if (user.IsFailure)
        {
            return Result.Failure<RecoveryCodesResponse>(user.Error!);
        }

        var verified = await secondFactor.VerifyAsync(user.Value!, code);
        if (verified.IsFailure)
        {
            return Result.Failure<RecoveryCodesResponse>(verified.Error!);
        }

        var codes = new RecoveryCodesResponse(await recoveryCodes.ReplaceAsync(user.Value!));

        // F-21 BR1: the previous codes stopped working here.
        await accountEvents.RecordAsync(user.Value!.Id, AccountEventType.RecoveryCodesRegenerated);
        return Result.Success(codes);
    }

    /// <summary>
    /// BR8: the current password and a code, both, so a stolen password alone cannot disarm the protection. A wrong
    /// password counts on the same lockout as sign-in; nothing changes unless both are right.
    /// </summary>
    public async Task<Result> DisableAsync(Guid userId, string? currentPassword, string? code)
    {
        var found = await EnabledUserAsync(userId);
        if (found.IsFailure)
        {
            return Result.Failure(found.Error!);
        }

        var user = found.Value!;

        // F-20 BR11: an account created with Google has no current password to prove; it sets one through the reset.
        if (!await userManager.HasPasswordAsync(user))
        {
            return Result.Failure(new Error(IdentityErrorCodes.PasswordNotSet, ErrorKind.BusinessRule));
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return await secondFactor.LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            if (!await userManager.IsLockedOutAsync(user))
            {
                return Result.Failure(new Error(IdentityErrorCodes.TotpCurrentPasswordInvalid, ErrorKind.BusinessRule));
            }

            // F-21 BR5: the refused attempt is not an event of its own, but the lockout it just caused is.
            await accountEvents.AccountLockedAsync(user.Id);
            return await secondFactor.LockedAsync(user);
        }

        var verified = await secondFactor.VerifyAsync(user, code);
        if (verified.IsFailure)
        {
            return verified;
        }

        user.DisableTotp();
        await userManager.UpdateAsync(user);
        await recoveryCodes.RemoveAsync(user);

        // F-21 BR1: the account lost its second factor; the most worth noticing of the account events.
        await accountEvents.RecordAsync(user.Id, AccountEventType.TwoFactorDisabled);
        return Result.Success();
    }

    private async Task<Result<User>> EnabledUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<User>(new Error(IdentityErrorCodes.UserNotFound, ErrorKind.NotFound));
        }

        return user.TwoFactorEnabled
            ? Result.Success(user)
            : Result.Failure<User>(new Error(IdentityErrorCodes.TotpNotEnabled, ErrorKind.Conflict));
    }
}
