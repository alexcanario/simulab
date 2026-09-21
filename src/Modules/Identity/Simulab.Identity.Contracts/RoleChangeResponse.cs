namespace Simulab.Identity.Contracts;

/// <summary>
/// One entry of the role history (F-14, BR2). <paramref name="Action"/> is <c>RoleCreated</c>, <c>RoleUpdated</c>,
/// <c>RoleDeleted</c> or <c>UserRolesChanged</c>. <paramref name="Role"/> carries the name the role had at that
/// moment; <paramref name="NameBefore"/> and <paramref name="NameAfter"/> are set only on a rename.
/// </summary>
public sealed record RoleChangeResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    RoleChangeUserResponse? Author,
    string Action,
    UserRoleResponse? Role,
    RoleChangeUserResponse? TargetUser,
    string? NameBefore,
    string? NameAfter,
    IReadOnlyList<RoleChangeItemResponse> Added,
    IReadOnlyList<RoleChangeItemResponse> Removed);
