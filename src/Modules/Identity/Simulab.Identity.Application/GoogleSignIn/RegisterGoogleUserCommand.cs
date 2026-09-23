namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>The confirmation of a first Google sign-in (F-20 BR7), with what only the host knows: the client address and the locale.</summary>
public sealed record RegisterGoogleUserCommand(
    string? IdToken,
    bool DeclaresAdult,
    bool AcceptsTerms,
    bool AcceptsPrivacy,
    string TermsVersion,
    string PrivacyVersion,
    string Locale,
    string? FullName = null,
    string? IpAddress = null);
