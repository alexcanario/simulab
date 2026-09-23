namespace Simulab.Identity.Contracts;

/// <summary>The values of <see cref="AccountEventResponse.Reason"/> (F-21, BR4); the screen translates them by key.</summary>
public static class AccountEventReasons
{
    public const string UnknownAccount = "UnknownAccount";
    public const string WrongPassword = "WrongPassword";
    public const string LockedOut = "LockedOut";
    public const string WrongCode = "WrongCode";
    public const string ChallengeInvalid = "ChallengeInvalid";
    public const string EmailNotVerified = "EmailNotVerified";
    public const string GoogleTokenRefused = "GoogleTokenRefused";

    public static readonly IReadOnlyList<string> All =
    [
        UnknownAccount,
        WrongPassword,
        LockedOut,
        WrongCode,
        ChallengeInvalid,
        EmailNotVerified,
        GoogleTokenRefused
    ];
}
