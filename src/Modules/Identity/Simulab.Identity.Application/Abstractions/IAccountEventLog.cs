using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Writes the account event trail (F-21). The client address comes from the request being served, so a caller
/// never passes it. Recording never fails the request that caused it (BR7): a write that throws is logged and
/// swallowed, because an audit row is worth less than the sign-in it would break.
/// </summary>
public interface IAccountEventLog
{
    /// <summary>BR3: the last step of a sign-in issued the tokens, passed the way given here.</summary>
    Task SignInSucceededAsync(Guid userId, AccountEventMethod method, CancellationToken cancellationToken = default);

    /// <summary>BR4: <paramref name="userId"/> is the account, or null when no account matched the attempt.</summary>
    Task SignInFailedAsync(Guid? userId, AccountEventReason reason, CancellationToken cancellationToken = default);

    /// <summary>BR5: only when this failure crossed the limit.</summary>
    Task AccountLockedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Everything that is neither a sign-in nor a lockout (BR1).</summary>
    Task RecordAsync(Guid userId, AccountEventType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the erasure event on the transaction the caller already opened instead of saving it (BR7): a
    /// rollback takes the event with it. It is saved by that transaction's own <c>SaveChanges</c>.
    /// </summary>
    void StageAccountErased(Guid userId);
}
