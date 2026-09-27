namespace Simulab.Identity.Contracts;

/// <summary>The Identity section of the data export (F-16 BR4). Never a secret or security material (BR5).</summary>
public sealed record IdentityDataResponse(
    AccountDataResponse Account,
    IReadOnlyList<string> Roles,
    IReadOnlyList<ConsentDataResponse> Consents,
    IReadOnlyList<RoleChangeDataResponse> RoleChanges,
    SessionDataResponse Sessions,
    IReadOnlyList<AccountEventDataResponse> AccountEvents);
