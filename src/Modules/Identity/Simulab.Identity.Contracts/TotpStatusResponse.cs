namespace Simulab.Identity.Contracts;

/// <summary>What the Security page reads (F-11): whether two-factor is on, since when, and how many recovery codes are left.</summary>
public sealed record TotpStatusResponse(bool Enabled, DateTimeOffset? EnabledAt = null, int RecoveryCodesLeft = 0);
