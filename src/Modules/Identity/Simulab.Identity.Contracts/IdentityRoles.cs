namespace Simulab.Identity.Contracts;

/// <summary>The seed roles (F-6, BR1; Grader since F-150). Global in v1; system roles in the roles screen (F-9).</summary>
public static class IdentityRoles
{
    public const string Student = "Student";
    public const string Curator = "Curator";
    public const string Admin = "Admin";
    public const string Grader = "Grader";

    public static readonly IReadOnlyList<string> All = [Student, Curator, Admin, Grader];
}
