namespace Simulab.Identity.Contracts;

/// <summary>
/// F-29: whether the signed-in account has a Google link, and which address it is.
/// </summary>
/// <param name="Linked">Whether the account has a Google login row.</param>
/// <param name="Email">
/// The Google address, or null when there is no link — and also null for a link written before F-29, whose
/// display name is still the provider's own name and holds no address to show (BR12).
/// </param>
public sealed record GoogleLinkResponse(bool Linked, string? Email = null);
