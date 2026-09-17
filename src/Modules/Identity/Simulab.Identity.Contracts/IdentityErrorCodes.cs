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
    public const string RegistrationRateLimited = "registration.rate_limited";

    public const string VerificationInvalid = "email_verification.invalid";
    public const string VerificationExpired = "email_verification.expired";
    public const string VerificationRateLimited = "email_verification.rate_limited";

    public const string LegalDocumentNotFound = "legal_document.not_found";
}
