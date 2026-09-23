using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// One recorded account or security event (F-21): the trail of what happened to an account. The time is the audit
/// field <see cref="TenantEntity.CreatedAt"/>, filled on save; <see cref="UserId"/> is the account the event is
/// about, not its author — a failed sign-in has no signed-in author, so <see cref="TenantEntity.CreatedBy"/> is
/// never read here. It holds no email, no name and no user agent (BR2), and it is never changed after it is
/// written (BR8) — except for <see cref="IpAddress"/>, which the erasure of that account clears (BR9).
/// </summary>
public sealed class AccountEvent : TenantEntity
{
    private AccountEvent()
    {
    }

    /// <summary>The account the event is about; null when no account matched the attempt (BR4).</summary>
    public Guid? UserId { get; private set; }

    public AccountEventType Type { get; private set; }

    /// <summary>How the sign-in was passed; null for everything that is not a sign-in (BR2).</summary>
    public AccountEventMethod? Method { get; private set; }

    /// <summary>Why it failed; null when nothing failed (BR2).</summary>
    public AccountEventReason? Reason { get; private set; }

    /// <summary>The client's address, when the host knows it. The one personal field of the event (BR9).</summary>
    public string? IpAddress { get; private set; }

    /// <summary>BR3: written when the last step issues the tokens, with the way that step was passed.</summary>
    public static AccountEvent SignInSucceeded(Guid userId, AccountEventMethod method, string? ipAddress) =>
        new()
        {
            UserId = userId,
            Type = AccountEventType.SignInSucceeded,
            Method = method,
            IpAddress = ipAddress
        };

    /// <summary>BR4: <c>userId</c> is the account, or null when no account matched the attempt.</summary>
    public static AccountEvent SignInFailed(Guid? userId, AccountEventReason reason, string? ipAddress) =>
        new()
        {
            UserId = userId,
            Type = AccountEventType.SignInFailed,
            Reason = reason,
            IpAddress = ipAddress
        };

    /// <summary>BR5: only when the failure crossed the limit — not locked before the attempt, locked after it.</summary>
    public static AccountEvent AccountLocked(Guid userId, string? ipAddress) =>
        new()
        {
            UserId = userId,
            Type = AccountEventType.AccountLocked,
            IpAddress = ipAddress
        };

    /// <summary>
    /// Everything that is neither a sign-in nor a lockout: signed out, the password events, the two-factor
    /// events and the erasure. Those three have their own factory because they carry a way or a reason.
    /// </summary>
    public static AccountEvent For(Guid userId, AccountEventType type, string? ipAddress)
    {
        if (type is AccountEventType.SignInSucceeded or AccountEventType.SignInFailed or AccountEventType.AccountLocked)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Sign-in and lockout events have their own factory.");
        }

        return new AccountEvent
        {
            UserId = userId,
            Type = type,
            IpAddress = ipAddress
        };
    }
}
