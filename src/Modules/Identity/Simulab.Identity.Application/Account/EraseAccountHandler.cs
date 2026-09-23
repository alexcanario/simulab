using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Account;

/// <summary>
/// A user erases their own account (F-10 UC2). The password check and the lockout are the ones a password
/// change uses (F-7 BR8); the writes run inside the role-administration transaction so the last-manager
/// check (BR11) judges the account list after the erasure and rolls it back when nobody would be left.
/// </summary>
public sealed class EraseAccountHandler(
    UserManager<User> userManager,
    IAccountErasureStore store,
    IRoleAdministrationStore roleStore,
    IRefreshSessionStore sessions,
    IErasureMailer mailer,
    IIntegrationEventPublisher events,
    TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(Guid userId, string? currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            // The account is already gone (BR14); the caller's token outlived it.
            return Failure(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid, ErrorKind.BusinessRule);
        }

        // F-20 BR11: an account created with Google has no current password to prove; it sets one through the reset.
        if (!await userManager.HasPasswordAsync(user))
        {
            return Failure(IdentityErrorCodes.PasswordNotSet, ErrorKind.BusinessRule);
        }

        // BR2: while locked, even the right current password is refused, as on sign-in.
        if (await userManager.IsLockedOutAsync(user))
        {
            return await LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user)
                ? await LockedAsync(user)
                : Failure(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid, ErrorKind.BusinessRule);
        }

        // BR12: read before the tombstone replaces them.
        var address = user.Email!;
        var locale = user.PreferredLanguage;
        var erasedAt = timeProvider.GetUtcNow();

        var erased = await roleStore.RunExclusiveAsync(
            () => EraseAsync(userId, address, erasedAt, locale, cancellationToken), cancellationToken);
        if (erased.IsFailure)
        {
            return Result.Failure(erased.Error!);
        }

        // BR10: every device of this account stops, the caller's included.
        await sessions.RevokeAllAsync(userId, cancellationToken: cancellationToken);

        // BR13: after the data changed, so a consumer never sees an account that is still there.
        await events.PublishAsync(new UserErased(userId, erasedAt), cancellationToken);

        return Result.Success();
    }

    /// <summary>The whole erasure, in one transaction (BR4, BR8, BR9, BR11), farewell email included.</summary>
    private async Task<Result<bool>> EraseAsync(
        Guid userId,
        string address,
        DateTimeOffset erasedAt,
        string locale,
        CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<bool>(new Error(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid, ErrorKind.BusinessRule));
        }

        // BR11: what the erasure has to leave behind. Counted before, so erasing an ordinary account in a
        // system that has no manager at all is not blamed for a hole it did not open.
        var managersBefore = await roleStore.CountActiveManagersAsync(cancellationToken);

        await store.RemoveAccountDataAsync(userId, cancellationToken);
        await store.ClearConsentAddressesAsync(userId, cancellationToken);

        var tombstone = user.Erase();
        store.ApplyErasure(user, tombstone);

        // BR12 through F-13 BR2: the farewell email is staged on this very transaction. An erasure that
        // rolls back below takes the job with it, and no job row outlives the address it was written for.
        await mailer.SendAccountErasedAsync(address, erasedAt, locale, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);

        // Judged after the change, inside the same transaction, which rolls back on failure.
        return managersBefore > 0 && await roleStore.CountActiveManagersAsync(cancellationToken) == 0
            ? Result.Failure<bool>(new Error(IdentityErrorCodes.AccountErasureLastManager, ErrorKind.BusinessRule))
            : Result.Success(true);
    }

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds, as a password change does.</summary>
    private async Task<Result> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    private static Result Failure(string code, ErrorKind kind) => Result.Failure(new Error(code, kind));
}
