namespace Simulab.Identity.Contracts;

/// <summary>Ten new single-use recovery codes (F-11 BR5, BR7). This answer is the only place they ever appear in plain text.</summary>
public sealed record RecoveryCodesResponse(IReadOnlyList<string> Codes);
