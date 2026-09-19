namespace Simulab.Identity.Contracts;

/// <summary>What a reset token check found (F-7 BR2, BR5).</summary>
public enum PasswordResetTokenStatus
{
    Valid,
    Invalid,
    Expired
}
