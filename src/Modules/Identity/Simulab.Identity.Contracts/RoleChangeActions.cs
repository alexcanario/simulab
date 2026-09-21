namespace Simulab.Identity.Contracts;

/// <summary>The values of <see cref="RoleChangeResponse.Action"/> (F-14, BR1); the screen translates them by key.</summary>
public static class RoleChangeActions
{
    public const string RoleCreated = "RoleCreated";
    public const string RoleUpdated = "RoleUpdated";
    public const string RoleDeleted = "RoleDeleted";
    public const string UserRolesChanged = "UserRolesChanged";

    public static readonly IReadOnlyList<string> All = [RoleCreated, RoleUpdated, RoleDeleted, UserRolesChanged];
}
