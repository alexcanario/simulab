namespace Simulab.Identity.Contracts;

/// <summary>
/// One account in the back office user list (F-9, UC5). <paramref name="Status"/> is the account status name;
/// <paramref name="TenantId"/> is the account's tenant, null when it has none (F-150, BR8).
/// </summary>
public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string? FullName,
    string Status,
    IReadOnlyList<UserRoleResponse> Roles,
    Guid? TenantId = null);
