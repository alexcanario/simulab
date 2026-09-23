namespace Simulab.Identity.Domain.Entities;

/// <summary>Why a sign-in failed (F-21, BR4). Stored as text.</summary>
public enum AccountEventReason
{
    /// <summary>No account matched the user name; the typed name is never stored.</summary>
    UnknownAccount,
    WrongPassword,
    LockedOut,
    WrongCode,
    ChallengeInvalid,
    EmailNotVerified,
    GoogleTokenRefused
}
