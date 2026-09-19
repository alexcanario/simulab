namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// What an account may do. An account is created <see cref="Pending"/> and becomes <see cref="Active"/>
/// when the email is verified (F-4); <see cref="Erased"/> is where it ends when the owner erases it
/// (F-10). Blocking and suspension arrive with the feature that needs them (F-4, decision of 2026-09-17).
/// </summary>
public enum AccountStatus
{
    /// <summary>Created, email not verified yet. Cannot sign in (F-5).</summary>
    Pending = 0,

    /// <summary>Email verified.</summary>
    Active = 1,

    /// <summary>
    /// Erased by its owner (F-10, BR6): the personal columns are overwritten and the row is soft deleted,
    /// so every query already hides it. The status is what a lookup that ignores the soft-delete filter
    /// sees, and it is final: nothing moves an account out of it.
    /// </summary>
    Erased = 2
}
