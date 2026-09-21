namespace Simulab.Identity.Contracts;

/// <summary>
/// What the role history's filters offer (F-14, BR8): every role and every author that appears in the trail,
/// and, when asked for, the user the page is filtered to (its email for the filter chip).
/// </summary>
public sealed record RoleChangeFiltersResponse(
    IReadOnlyList<RoleChangeFilterRoleResponse> Roles,
    IReadOnlyList<RoleChangeUserResponse> Authors,
    RoleChangeUserResponse? User);
