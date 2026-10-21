using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.LegalAcceptance;

/// <summary>
/// The current terms and privacy documents for one locale (F-4 BR14: the version code is the same in every
/// locale). A document the server cannot read at all is null, and its version is never compared: a
/// server-side problem is not the visitor's (F-4 BR5, F-85 BR2).
/// </summary>
/// <param name="Terms">The current terms, or null when the server cannot read them.</param>
/// <param name="Privacy">The current privacy policy, or null when the server cannot read it.</param>
public sealed record CurrentLegalDocuments(LegalDocumentResponse? Terms, LegalDocumentResponse? Privacy)
{
    /// <summary>True when a document that can be read has a version other than the one given. One place for sign-up and for F-85.</summary>
    public bool Differs(string termsVersion, string privacyVersion) =>
        (Terms is not null && !string.Equals(Terms.Version, termsVersion, StringComparison.Ordinal))
        || (Privacy is not null && !string.Equals(Privacy.Version, privacyVersion, StringComparison.Ordinal));
}
