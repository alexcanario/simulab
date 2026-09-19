using Simulab.Identity.Contracts;

namespace Simulab.Web.Components.Pages.Admin;

/// <summary>One line of the roles table: the role and its name as shown (translated for a system role).</summary>
public sealed record RoleRow(RoleResponse Role, string DisplayName)
{
    public int PermissionCount => Role.Permissions.Count;

    public int UserCount => Role.UserCount;
}
