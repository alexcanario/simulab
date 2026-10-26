namespace Simulab.Identity.Contracts;

/// <summary>The tenant a user belongs to after the call (F-150, BR5); null clears it.</summary>
public sealed record SetUserTenantRequest(Guid? TenantId = null);
