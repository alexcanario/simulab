namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// Every checkable permission of the ExamEngine module (F-166). The name is also the policy suffix:
/// <c>$"Permission:{name}"</c>.
/// </summary>
public static class ExamEnginePermissions
{
    /// <summary>Starts an attempt, answers its questions and submits it; the <c>Student</c> role holds it from the start.</summary>
    public const string Take = "attempts.take";

    /// <summary>Reads any student's attempt, for support (F-166, BR7); granted to <c>Admin</c> only, and never changes one.</summary>
    public const string Read = "attempts.read";

    /// <summary>The module name Identity seeds this catalog under.</summary>
    public const string ModuleName = "attempts";

    public static readonly IReadOnlyList<string> All = [Take, Read];
}
