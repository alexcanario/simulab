namespace Simulab.Identity.Application.Totp;

/// <summary>Which of the two codes a second-factor check accepted (F-11 BR6). F-21 records it on a sign-in.</summary>
public enum SecondFactorMethod
{
    TotpCode,
    RecoveryCode
}
