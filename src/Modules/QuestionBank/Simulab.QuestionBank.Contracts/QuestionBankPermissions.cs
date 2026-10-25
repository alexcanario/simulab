namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// Every checkable permission of the QuestionBank module (F-95, BR14). The name is also the policy suffix:
/// <c>$"Permission:{name}"</c>. Review has its own permission (F-105).
/// </summary>
public static class QuestionBankPermissions
{
    /// <summary>Gates the question bank back office: authoring, publishing, listing and previewing questions.</summary>
    public const string Manage = "questions.manage";

    /// <summary>Gates the review queue and the decision on a question in review (F-105, BR10); a holder needs no <see cref="Manage"/>.</summary>
    public const string Review = "questions.review";

    /// <summary>Grades essays and open answers (F-150, BR2); nothing requires it until the correction queue (F-151).</summary>
    public const string Grade = "questions.grade";

    /// <summary>The module name Identity seeds this catalog under.</summary>
    public const string ModuleName = "questions";

    public static readonly IReadOnlyList<string> All = [Manage, Review];
}
