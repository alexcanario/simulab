namespace Simulab.Identity.Contracts;

/// <summary>
/// One current document in the acceptance status (F-85), in the caller's locale.
/// </summary>
/// <param name="Topic">Terms or privacy.</param>
/// <param name="Version">The current version code, the same in every locale.</param>
/// <param name="EffectiveDate">The day this version came into force.</param>
/// <param name="Title">The document title, from the manifest.</param>
/// <param name="IsPlaceholder">True while the text is a draft under review.</param>
/// <param name="Changed">True when this version is not the one in the user's last acceptance (always, when there is none).</param>
public sealed record LegalAcceptanceDocumentResponse(
    LegalTopic Topic,
    string Version,
    DateOnly? EffectiveDate,
    string Title,
    bool IsPlaceholder,
    bool Changed);
