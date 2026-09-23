namespace Simulab.Identity.Contracts;

/// <summary>
/// Every failure code this module can return (rule: api-contracts). A code is stable: it is the
/// resource key the UI translates, so it is never renamed or reused.
/// </summary>
public static class IdentityErrorCodes
{
    public const string AgeDeclarationRequired = "registration.age_declaration_required";
    public const string ConsentRequired = "registration.consent_required";
    public const string TermsVersionOutdated = "registration.terms_version_outdated";
    public const string PasswordTooWeak = "registration.password_too_weak";
    public const string EmailInvalid = "registration.email_invalid";
    public const string RegistrationFullNameTooLong = "registration.full_name_too_long";
    public const string RegistrationRateLimited = "registration.rate_limited";

    public const string VerificationInvalid = "email_verification.invalid";
    public const string VerificationExpired = "email_verification.expired";
    public const string VerificationRateLimited = "email_verification.rate_limited";

    public const string LegalDocumentNotFound = "legal_document.not_found";

    public const string InvalidCredentials = "identity.invalid_credentials";
    public const string EmailNotVerified = "identity.email_not_verified";
    public const string AccountLocked = "identity.account_locked";
    public const string RefreshTokenInvalid = "identity.refresh_token_invalid";
    public const string TokenRevoked = "identity.token_revoked";
    public const string Forbidden = "identity.forbidden";

    public const string PasswordResetInvalid = "password_reset.invalid";
    public const string PasswordResetExpired = "password_reset.expired";
    public const string PasswordResetRateLimited = "password_reset.rate_limited";
    public const string PasswordResetTooWeak = "password_reset.password_too_weak";
    public const string PasswordResetSameAsCurrent = "password_reset.same_as_current";

    public const string PasswordChangeCurrentInvalid = "password_change.current_password_invalid";
    public const string PasswordChangeTooWeak = "password_change.password_too_weak";
    public const string PasswordChangeSameAsCurrent = "password_change.same_as_current";

    public const string AccountErasureCurrentPasswordInvalid = "account_erasure.current_password_invalid";
    public const string AccountErasureLastManager = "account_erasure.last_manager";

    public const string DataExportCurrentPasswordInvalid = "data_export.current_password_invalid";

    public const string ProfileFullNameTooLong = "profile.full_name_too_long";
    public const string ProfileLanguageNotSupported = "profile.language_not_supported";

    public const string RoleNameInvalid = "role.name_invalid";
    public const string RoleNameTaken = "role.name_taken";
    public const string RolePermissionUnknown = "role.permission_unknown";
    public const string RoleNotFound = "role.not_found";
    public const string RoleSystemRoleProtected = "role.system_role_protected";
    public const string RoleAdminPermissionRequired = "role.admin_permission_required";
    public const string RoleInUse = "role.in_use";

    public const string RoleAssignmentRoleUnknown = "role_assignment.role_unknown";
    public const string RoleAssignmentLastManager = "role_assignment.last_manager";

    public const string UserNotFound = "user.not_found";

    /// <summary>F-14, BR6: the history's period is 7, 30 or 90 days, or absent for all time.</summary>
    public const string RoleChangePeriodInvalid = "role_change.period_invalid";

    /// <summary>F-21, BR10: the trail's period is 7, 30 or 90 days, or absent for all time.</summary>
    public const string AccountEventPeriodInvalid = "account_event.period_invalid";

    /// <summary>F-21, BR10: the event filter is one of <see cref="AccountEventTypes.All"/>.</summary>
    public const string AccountEventTypeInvalid = "account_event.event_invalid";

    /// <summary>
    /// F-11 BR9: the password was right and the account has two-factor on. An OAuth <c>error</c> of the token
    /// endpoint, not a failure the user reads: the sign-in page moves to the code step.
    /// </summary>
    public const string TotpRequired = "mfa_required";

    public const string TotpCodeInvalid = "totp.code_invalid";
    public const string TotpChallengeInvalid = "totp.challenge_invalid";
    public const string TotpAlreadyEnabled = "totp.already_enabled";
    public const string TotpNotEnabled = "totp.not_enabled";
    public const string TotpCurrentPasswordInvalid = "totp.current_password_invalid";

    /// <summary>F-20 BR11: the action asks for the current password and the account has none (created with Google).</summary>
    public const string PasswordNotSet = "identity.password_not_set";

    /// <summary>F-20 BR2: the Google ID token failed a check (issuer, audience, signature, lifetime).</summary>
    public const string GoogleTokenInvalid = "google_sign_in.invalid_token";

    /// <summary>F-20 BR2: Google did not mark the address as verified.</summary>
    public const string GoogleEmailNotVerified = "google_sign_in.email_not_verified";

    /// <summary>
    /// F-20 BR7: no account for this Google identity. An OAuth <c>error</c> of the token endpoint, not a failure the
    /// user reads: the Web host moves to the confirmation page.
    /// </summary>
    public const string GoogleSignUpRequired = "google_sign_in.sign_up_required";

    /// <summary>F-20 BR9: an account for this address or Google subject appeared before the confirmation.</summary>
    public const string GoogleAccountExists = "google_sign_in.account_exists";

    /// <summary>F-20 BR8: the Web host's waiting-confirmation ticket expired or was already used.</summary>
    public const string GoogleSignInExpired = "google_sign_in.expired";
}
