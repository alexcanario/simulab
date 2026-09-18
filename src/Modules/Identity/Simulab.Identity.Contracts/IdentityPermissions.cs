namespace Simulab.Identity.Contracts;

/// <summary>Every checkable permission (F-6, BR3). The name is also the policy suffix: <c>$"Permission:{name}"</c>.</summary>
public static class IdentityPermissions
{
    /// <summary>Reserved for F-9 (role management back office); gates the placeholder <c>/admin/roles</c> in F-6 (BR8).</summary>
    public const string RolesManage = "identity.roles.manage";

    public static readonly IReadOnlyList<string> All = [RolesManage];
}
