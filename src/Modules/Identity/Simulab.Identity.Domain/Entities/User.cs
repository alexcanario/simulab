using Microsoft.AspNetCore.Identity;
using Simulab.SharedKernel.Entities;

namespace Simulab.Identity.Domain.Entities;

/// <summary>
/// The account aggregate. It inherits <see cref="IdentityUser{TKey}"/> (ADR-0001, decision 12), so it
/// cannot also inherit <see cref="TenantEntity"/> and repeats its tenant, audit and soft-delete fields:
/// the declared exception of the profile. The audit interceptor works on the interfaces, so the fields
/// are filled the same way as in any other entity.
/// </summary>
public class User : IdentityUser<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    /// <summary>Null for global and B2C accounts. Dormant in v1 (ADR-0001, decision 7).</summary>
    public Guid? TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    /// <summary>Starts <see cref="AccountStatus.Pending"/>; only <see cref="VerifyEmail"/> moves it on.</summary>
    public AccountStatus Status { get; private set; } = AccountStatus.Pending;

    /// <summary>When the email was verified; null while the account is pending.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    /// <summary>Optional display name (F-4, BR1). Blank input is stored as null.</summary>
    public string? FullName { get; set; }

    /// <summary>The 18+ self-declaration made at sign-up (ADR-0001, decision 17). The auditable source is the consent record.</summary>
    public bool IsAdultDeclared { get; set; }

    /// <summary>The locale this user's emails are written in (F-4, BR7). Editable from F-8.</summary>
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// The only way to <see cref="AccountStatus.Active"/> (BR9). Verifying an account that is already
    /// active changes nothing, so a repeated link is not an error (BR10).
    /// </summary>
    public void VerifyEmail(DateTimeOffset verifiedAt)
    {
        if (Status == AccountStatus.Active)
        {
            return;
        }

        Status = AccountStatus.Active;
        EmailVerifiedAt = verifiedAt;
        EmailConfirmed = true;
    }
}
