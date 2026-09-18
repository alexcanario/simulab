namespace Simulab.Identity.Contracts;

/// <summary>
/// The dynamic ASP.NET Core authorization policy name for a permission (F-6, BR3): both the Api's
/// policy provider and the Web's <c>AuthorizeView</c>/<c>AuthorizeRouteView</c> checks use the same
/// prefix, so a permission name never needs its own hand-written policy.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "Permission:";

    public static string NameFor(string permission) => Prefix + permission;
}
