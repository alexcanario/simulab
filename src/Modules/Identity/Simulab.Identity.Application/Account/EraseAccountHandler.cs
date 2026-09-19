using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
    TimeProvider timeProvider,
    ILogger<EraseAccountHandler> logger)
{
    public async Task<Result> HandleAsync(Guid userId, string? currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            // The account is already gone (BR14); the caller's token outlived it.
            return Failure(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid, ErrorKind.BusinessRule);
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

        var erased = await roleStore.RunExclusiveAsync(() => EraseAsync(userId, cancellationToken), cancellationToken);
        if (erased.IsFailure)
        {
            return Result.Failure(erased.Error!);
        }

        // BR10: every device of this account stops, the caller's included.
        await sessions.RevokeAllAsync(userId, cancellationToken: cancellationToken);

        await NotifyAsync(address, erasedAt, locale, cancellationToken);

        // BR13: after the data changed, so a consumer never sees an account that is still there.
        await events.PublishAsync(new UserErased(userId, erasedAt), cancellationToken);

        return Result.Success();
    }

    /// <summary>The whole erasure, in one transaction (BR4, BR8, BR9, BR11).</summary>
    private async Task<Result<bool>> EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<bool>(new Error(IdentityErrorCodes.AccountErasureCurrentPasswordInvalid, ErrorKind.BusinessRule));
        }

        await store.RemoveAccountDataAsync(userId, cancellationToken);
        await store.ClearConsentAddressesAsync(userId, cancellationToken);

        var tombstone = user.Erase();
        store.ApplyErasure(user, tombstone);
        await store.SaveChangesAsync(cancellationToken);

        // BR11: judged after the change, inside the same transaction, which rolls back on failure.
        return await roleStore.CountActiveManagersAsync(cancellationToken) == 0
            ? Result.Failure<bool>(new Error(IdentityErrorCodes.AccountErasureLastManager, ErrorKind.BusinessRule))
            : Result.Success(true);
    }

    /// <summary>
    /// BR12, best effort: the account is already gone when this runs. A mail server that fails is logged,
    /// never reported as a failed erasure, or the user would try again on an account that no longer exists.
    /// </summary>
    private async Task NotifyAsync(string address, DateTimeOffset erasedAt, string locale, CancellationToken cancellationToken)
    {
        try
        {
            await mailer.SendAccountErasedAsync(address, erasedAt, locale, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sending the account-erased email failed; the erasure itself stands.");
        }
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
