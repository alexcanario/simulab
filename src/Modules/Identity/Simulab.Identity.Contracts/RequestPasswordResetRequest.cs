namespace Simulab.Identity.Contracts;

/// <summary>Asks for a reset link (F-7 UC1). The answer never says whether the address exists.</summary>
public sealed record RequestPasswordResetRequest(string? Email);
