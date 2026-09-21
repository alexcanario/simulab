namespace Simulab.Identity.Contracts;

/// <summary>A current code or a recovery code, which proves the second factor before new codes replace the old ones (F-11 BR7).</summary>
public sealed record RegenerateRecoveryCodesRequest(string? Code);
