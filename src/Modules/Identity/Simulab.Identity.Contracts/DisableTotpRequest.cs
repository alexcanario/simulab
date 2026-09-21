namespace Simulab.Identity.Contracts;

/// <summary>Turning two-factor off needs the current password and a code or recovery code (F-11 BR8).</summary>
public sealed record DisableTotpRequest(string? CurrentPassword, string? Code);
