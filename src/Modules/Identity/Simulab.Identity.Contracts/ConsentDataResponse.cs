namespace Simulab.Identity.Contracts;

/// <summary>One consent record in the data export (F-16 BR4), IP address included: it is the user's data.</summary>
public sealed record ConsentDataResponse(
    string TermsVersion,
    string PrivacyVersion,
    bool DeclaresAdult,
    string Locale,
    DateTimeOffset AcceptedAt,
    string? IpAddress);
