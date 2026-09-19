namespace Simulab.Identity.Contracts;

/// <summary>Every checkable permission (F-6, BR3). The name is also the policy suffix: <c>$"Permission:{name}"</c>.</summary>
public static class IdentityPermissions
{
    /// <summary>Gates the role management back office: roles, users and role assignments (F-9, BR10).</summary>
    public const string RolesManage = "identity.roles.manage";

    public static readonly IReadOnlyList<string> All = [RolesManage];

    /// <summary>The module a permission belongs to: its name up to the first dot (F-9, BR12).</summary>
    public static string GroupOf(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        var dot = permission.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? permission : permission[..dot];
    }
}
