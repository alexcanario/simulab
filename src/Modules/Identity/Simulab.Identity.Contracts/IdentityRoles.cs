namespace Simulab.Identity.Contracts;

/// <summary>The three seed roles (F-6, BR1). Global in v1; no CRUD screen exists yet (F-9).</summary>
public static class IdentityRoles
{
    public const string Student = "Student";
    public const string Curator = "Curator";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Student, Curator, Admin];
}
