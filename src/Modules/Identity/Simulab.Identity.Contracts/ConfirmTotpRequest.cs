namespace Simulab.Identity.Contracts;

/// <summary>The first code from the authenticator app, which turns two-factor on (F-11 BR2).</summary>
public sealed record ConfirmTotpRequest(string? Code);
