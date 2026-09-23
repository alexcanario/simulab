namespace Simulab.Identity.Contracts;

/// <summary>
/// The confirmation page of a first Google sign-in (F-20 UC1, BR7). The identity is the Google ID token, which the
/// Api checks itself (BR2); the rest is what the password sign-up asks, with the versions the page showed.
/// </summary>
/// <param name="IdToken">The Google ID token the Web host received at the end of the round trip.</param>
/// <param name="DeclaresAdult">The 18+ self-declaration.</param>
/// <param name="AcceptsTerms">Acceptance of the terms of use.</param>
/// <param name="AcceptsPrivacy">Acceptance of the privacy policy.</param>
/// <param name="TermsVersion">The terms version the page showed.</param>
/// <param name="PrivacyVersion">The privacy version the page showed.</param>
/// <param name="FullName">The name from Google, as the visitor left it; optional.</param>
public sealed record GoogleRegistrationRequest(
    string IdToken,
    bool DeclaresAdult,
    bool AcceptsTerms,
    bool AcceptsPrivacy,
    string TermsVersion,
    string PrivacyVersion,
    string? FullName = null);
