namespace Simulab.Identity.Contracts;

/// <summary>Asks for a new verification link. The answer is always the same, whatever the address is (BR11).</summary>
public sealed record ResendVerificationRequest(string Email);
