namespace Simulab.Identity.Contracts;

/// <summary>
/// The current version of one institutional document, in one locale (BR14).
/// </summary>
/// <param name="Topic">Terms or privacy.</param>
/// <param name="Locale">The locale the body is written in.</param>
/// <param name="Version">The version code, the same in every locale.</param>
/// <param name="EffectiveDate">The day this version came into force.</param>
/// <param name="Title">The document title, from the manifest.</param>
/// <param name="BodyHtml">The body, already rendered from Markdown to sanitized HTML.</param>
/// <param name="IsPlaceholder">True while the text is a draft under review (BR15).</param>
public sealed record LegalDocumentResponse(
    LegalTopic Topic,
    string Locale,
    string Version,
    DateOnly? EffectiveDate,
    string Title,
    string BodyHtml,
    bool IsPlaceholder);
