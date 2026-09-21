using Microsoft.AspNetCore.Identity;
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

        return Result.Success(new RecoveryCodesResponse(await recoveryCodes.ReplaceAsync(user)));
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
        return verified.IsFailure
            ? Result.Failure<RecoveryCodesResponse>(verified.Error!)
            : Result.Success(new RecoveryCodesResponse(await recoveryCodes.ReplaceAsync(user.Value!)));
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
        if (await userManager.IsLockedOutAsync(user))
        {
            return await secondFactor.LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user)
                ? await secondFactor.LockedAsync(user)
                : Result.Failure(new Error(IdentityErrorCodes.TotpCurrentPasswordInvalid, ErrorKind.BusinessRule));
        }

        var verified = await secondFactor.VerifyAsync(user, code);
        if (verified.IsFailure)
        {
            return verified;
        }

        user.DisableTotp();
        await userManager.UpdateAsync(user);
        await recoveryCodes.RemoveAsync(user);
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
