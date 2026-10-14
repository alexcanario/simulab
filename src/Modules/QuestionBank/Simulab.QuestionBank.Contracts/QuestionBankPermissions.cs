namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// Every checkable permission of the QuestionBank module (F-95, BR14). The name is also the policy suffix:
/// <c>$"Permission:{name}"</c>. Review gets its own permission in F-105.
/// </summary>
public static class QuestionBankPermissions
{
    /// <summary>Gates the question bank back office: authoring, publishing, listing and previewing questions.</summary>
    public const string Manage = "questions.manage";

    /// <summary>The module name Identity seeds this catalog under.</summary>
    public const string ModuleName = "questions";

    public static readonly IReadOnlyList<string> All = [Manage];
}
