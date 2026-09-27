namespace Simulab.Identity.Contracts;

/// <summary>
/// The values of <see cref="AccountEventResponse.Event"/> (F-21, BR1); the screen translates them by key and the
/// endpoint refuses anything else (<see cref="IdentityErrorCodes.AccountEventTypeInvalid"/>). A test pins them
/// to the domain enum.
/// </summary>
public static class AccountEventTypes
{
    public const string SignInSucceeded = "SignInSucceeded";
    public const string SignInFailed = "SignInFailed";
    public const string AccountLocked = "AccountLocked";
    public const string SignedOut = "SignedOut";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string PasswordResetCompleted = "PasswordResetCompleted";
    public const string TwoFactorEnabled = "TwoFactorEnabled";
    public const string TwoFactorDisabled = "TwoFactorDisabled";
    public const string RecoveryCodesRegenerated = "RecoveryCodesRegenerated";
    public const string AccountErased = "AccountErased";

    /// <summary>F-29 BR13: the account's owner connected or disconnected Google.</summary>
    public const string GoogleLinked = "GoogleLinked";

    public const string GoogleUnlinked = "GoogleUnlinked";

    public static readonly IReadOnlyList<string> All =
    [
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
        GoogleLinked,
        GoogleUnlinked
    ];
}
