namespace Simulab.Identity.Contracts;

/// <summary>
/// The versions the page showed and the locale it showed them in (F-85 BR3, BR5). There is no user id on purpose:
/// the acceptance is always the signed-in caller's own.
/// </summary>
public sealed record AcceptLegalDocumentsRequest(string TermsVersion, string PrivacyVersion, string Locale);
