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
    /// The authenticator secret, encrypted (F-11 BR3). Set by an enrolment before two-factor is on (BR2);
    /// the plain secret never reaches the database.
    /// </summary>
    public string? TotpSecretEncrypted { get; private set; }

    /// <summary>When two-factor was turned on (F-11); null while it is off.</summary>
    public DateTimeOffset? TotpEnabledAt { get; private set; }

    /// <summary>The last 30-second step whose code was accepted (F-11 BR4). A code at or below it is a replay.</summary>
    public long? TotpLastAcceptedStep { get; private set; }

    /// <summary>
    /// F-11 BR2: stores a new secret for an enrolment. An earlier unconfirmed secret is simply replaced;
    /// two-factor stays off until <see cref="EnableTotp"/>.
    /// </summary>
    public void StartTotpEnrolment(string encryptedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedSecret);

        TotpSecretEncrypted = encryptedSecret;
        TotpLastAcceptedStep = null;
    }

    /// <summary>
    /// F-11 BR4: records <paramref name="step"/> as used. False when it is at or below the last accepted
    /// step, so a code works once even inside its 90-second window.
    /// </summary>
    public bool AcceptTotpStep(long step)
    {
        if (TotpLastAcceptedStep is { } last && step <= last)
        {
            return false;
        }

        TotpLastAcceptedStep = step;
        return true;
    }

    /// <summary>
    /// F-11 BR2: turns two-factor on after the first valid code. The flag is written here, not through
    /// <c>UserManager.SetTwoFactorEnabledAsync</c>, which renews the security stamp and would end every
    /// other session (BR11).
    /// </summary>
    public void EnableTotp(DateTimeOffset enabledAt)
    {
        TwoFactorEnabled = true;
        TotpEnabledAt = enabledAt;
    }

    /// <summary>F-11 BR8: clears the flag, the secret and the last step. The caller removes the recovery codes.</summary>
    public void DisableTotp()
    {
        TwoFactorEnabled = false;
        TotpEnabledAt = null;
        TotpSecretEncrypted = null;
        TotpLastAcceptedStep = null;
    }

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

    /// <summary>
    /// The address that replaces the real one when the account is erased (F-10, BR5). The TLD
    /// <c>.invalid</c> is reserved by RFC 2606, so it can never reach a mailbox, and the account id makes
    /// it unique: two erased accounts never collide under <c>ux_users_tenant_normalized_email</c>.
    /// </summary>
    public static string TombstoneAddressFor(Guid userId) => $"erased-{userId:N}@erased.invalid";

    /// <summary>
    /// Overwrites every personal column with the tombstone or nothing (F-10, BR4) and ends the account at
    /// <see cref="AccountStatus.Erased"/>. <see cref="IdentityUser{TKey}.Id"/>, <see cref="CreatedAt"/> and
    /// <see cref="PreferredLanguage"/> stay: the id is the pseudonym future statistics hang from
    /// (ADR-0001 #9) and the language is not personal data. The caller writes the normalized columns and
    /// soft deletes the row; returns the tombstone address it has to normalize.
    /// </summary>
    public string Erase()
    {
        var tombstone = TombstoneAddressFor(Id);

        Email = tombstone;
        UserName = tombstone;
        FullName = null;
        PhoneNumber = null;
        PhoneNumberConfirmed = false;

        // A null hash cannot be checked against any password, and a fresh stamp invalidates what the old one signed.
        PasswordHash = null;
        SecurityStamp = Guid.NewGuid().ToString();
        ConcurrencyStamp = Guid.NewGuid().ToString();

        LockoutEnd = null;
        AccessFailedCount = 0;

        // F-11 BR13: the second factor goes with the account; the recovery codes go with user_tokens.
        DisableTotp();

        EmailConfirmed = false;
        EmailVerifiedAt = null;
        IsAdultDeclared = false;

        Status = AccountStatus.Erased;
        return tombstone;
    }
}
