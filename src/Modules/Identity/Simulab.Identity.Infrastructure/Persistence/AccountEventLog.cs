using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Security;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// The account event trail (F-21, BR7). Every write is its own save: the account handlers have no transaction
/// of their own (the handlers that hold a transaction, F-30 and F-47, record theirs after the commit), and what
/// the event records has already been committed. The one exception is the erasure, which stages its event on
/// the transaction that erases the account.
/// </summary>
public sealed class AccountEventLog(
    IdentityModuleDbContext context,
    ICallerAddress callerAddress,
    ILogger<AccountEventLog> logger) : IAccountEventLog
{
    public Task SignInSucceededAsync(Guid userId, AccountEventMethod method, CancellationToken cancellationToken = default) =>
        SaveAsync(AccountEvent.SignInSucceeded(userId, method, callerAddress.Value), cancellationToken);

    public Task SignInFailedAsync(Guid? userId, AccountEventReason reason, CancellationToken cancellationToken = default) =>
        SaveAsync(AccountEvent.SignInFailed(userId, reason, callerAddress.Value), cancellationToken);

    public Task AccountLockedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        SaveAsync(AccountEvent.AccountLocked(userId, callerAddress.Value), cancellationToken);

    public Task RecordAsync(Guid userId, AccountEventType type, CancellationToken cancellationToken = default) =>
        SaveAsync(AccountEvent.For(userId, type, callerAddress.Value), cancellationToken);

    /// <summary>
    /// BR9: with no address. The same transaction clears the address of every other event of this account, and
    /// a row inserted by this save would not be seen by that statement — so the trail would end with the one
    /// address the erasure exists to remove.
    /// </summary>
    public void StageAccountErased(Guid userId) =>
        context.AccountEvents.Add(AccountEvent.For(userId, AccountEventType.AccountErased, ipAddress: null));

    /// <summary>
    /// BR7: the trail never breaks what it records. A failure here is a defect to fix, not a reason to refuse a
    /// sign-in or to undo a password change that already happened, so it is logged and swallowed.
    /// </summary>
    private async Task SaveAsync(AccountEvent accountEvent, CancellationToken cancellationToken)
    {
        try
        {
            context.AccountEvents.Add(accountEvent);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The failed insert stays Added on the context; a later save in this scope would try it again.
            context.Entry(accountEvent).State = EntityState.Detached;
            logger.LogError(exception, "The account event {EventType} could not be recorded.", accountEvent.Type);
        }
    }
}
