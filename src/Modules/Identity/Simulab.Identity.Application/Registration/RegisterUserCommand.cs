namespace Simulab.Identity.Application.Registration;

/// <summary>
/// A sign-up attempt, with what only the host knows: the client address and the locale the legal
/// documents were shown in.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    bool DeclaresAdult,
    bool AcceptsTerms,
    bool AcceptsPrivacy,
    string TermsVersion,
    string PrivacyVersion,
    string Locale,
    string? FullName = null,
    string? IpAddress = null);
