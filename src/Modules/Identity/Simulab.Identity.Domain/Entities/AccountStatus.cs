namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// What an account may do. F-4 uses only these two: an account is created <see cref="Pending"/> and
/// becomes <see cref="Active"/> when the email is verified. Blocking and suspension arrive with the
/// feature that needs them (F-4, decision of 2026-09-17).
/// </summary>
public enum AccountStatus
{
    /// <summary>Created, email not verified yet. Cannot sign in (F-5).</summary>
    Pending = 0,

    /// <summary>Email verified.</summary>
    Active = 1
}
