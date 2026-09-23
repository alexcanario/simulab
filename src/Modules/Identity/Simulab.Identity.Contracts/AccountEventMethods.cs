namespace Simulab.Identity.Contracts;

/// <summary>The values of <see cref="AccountEventResponse.Method"/> (F-21, BR2); the screen translates them by key.</summary>
public static class AccountEventMethods
{
    public const string Password = "Password";
    public const string Google = "Google";
    public const string TotpCode = "TotpCode";
    public const string RecoveryCode = "RecoveryCode";

    public static readonly IReadOnlyList<string> All = [Password, Google, TotpCode, RecoveryCode];
}
