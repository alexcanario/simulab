namespace Simulab.Identity.Domain.Entities;

/// <summary>What an <see cref="AccountEvent"/> records (F-21, BR1). Stored as text.</summary>
public enum AccountEventType
{
    SignInSucceeded,
    SignInFailed,
    AccountLocked,
    SignedOut,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    TwoFactorEnabled,
    TwoFactorDisabled,
    RecoveryCodesRegenerated,
    AccountErased,

    // F-29: the account's owner connected or disconnected Google on the Security page.
    GoogleLinked,
    GoogleUnlinked
}
