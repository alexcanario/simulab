namespace Simulab.Identity.Contracts;

/// <summary>
/// Whether the signed-in user owes an acceptance of the current legal documents (F-85 BR1).
/// </summary>
/// <param name="Owes">True when the user has no acceptance, or their last one is not of the current versions.</param>
/// <param name="Documents">The current documents the server could read; a document it cannot read is left out (BR2).</param>
public sealed record LegalAcceptanceStatusResponse(bool Owes, IReadOnlyList<LegalAcceptanceDocumentResponse> Documents);
