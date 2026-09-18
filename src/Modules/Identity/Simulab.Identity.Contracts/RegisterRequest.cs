namespace Simulab.Identity.Contracts;

/// <summary>
/// What the sign-up form sends. The document versions are the ones the page showed, so the server can
/// refuse a form filled against texts that have since changed (BR5).
/// </summary>
/// <param name="Email">The address that identifies the account.</param>
/// <param name="Password">The chosen password; the policy is checked by the server (BR2).</param>
/// <param name="DeclaresAdult">The 18+ self-declaration (BR1).</param>
/// <param name="AcceptsTerms">Acceptance of the terms of use.</param>
/// <param name="AcceptsPrivacy">Acceptance of the privacy policy.</param>
/// <param name="TermsVersion">The terms version the page showed.</param>
/// <param name="PrivacyVersion">The privacy version the page showed.</param>
/// <param name="FullName">Optional display name.</param>
public sealed record RegisterRequest(
    string Email,
    string Password,
    bool DeclaresAdult,
    bool AcceptsTerms,
    bool AcceptsPrivacy,
    string TermsVersion,
    string PrivacyVersion,
    string? FullName = null);
