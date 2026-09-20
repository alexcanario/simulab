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
}
