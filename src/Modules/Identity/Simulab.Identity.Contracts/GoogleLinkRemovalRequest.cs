namespace Simulab.Identity.Contracts;

/// <summary>F-29 BR8: disconnecting Google is confirmed with the account's current password.</summary>
/// <param name="CurrentPassword">The password the account signs in with.</param>
public sealed record GoogleLinkRemovalRequest(string? CurrentPassword);
