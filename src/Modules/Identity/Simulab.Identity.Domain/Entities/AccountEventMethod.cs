namespace Simulab.Identity.Domain.Entities;

/// <summary>How a sign-in was passed (F-21, BR2, BR3): the way the last step was proved. Stored as text.</summary>
public enum AccountEventMethod
{
    Password,
    Google,
    TotpCode,
    RecoveryCode
}
