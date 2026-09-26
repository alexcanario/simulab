namespace Simulab.Identity.Contracts;

/// <summary>F-29 BR2: the checked Google ID token whose identity is linked to the signed-in account.</summary>
/// <param name="IdToken">Google's ID token, as the round trip gave it back.</param>
public sealed record GoogleLinkRequest(string? IdToken);
