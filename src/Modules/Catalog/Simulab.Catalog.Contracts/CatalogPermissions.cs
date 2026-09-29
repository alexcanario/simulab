namespace Simulab.Catalog.Contracts;

/// <summary>
/// Every checkable permission of the Catalog module (F-33, BR2 and BR4). The name is also the policy
/// suffix: <c>$"Permission:{name}"</c>. One permission covers the whole module until Curator and Admin
/// really diverge (F-33, decision of 2026-09-23).
/// </summary>
public static class CatalogPermissions
{
    /// <summary>Gates the catalog back office: organizers today, exams and editions next (F-33, BR4).</summary>
    public const string Manage = "catalog.manage";

    /// <summary>
    /// Gates the student catalog: the published exams, their page and the filter options (F-36, BR10).
    /// Identity grants it once to Student, Curator and Admin, when the permission row is created.
    /// </summary>
    public const string Browse = "catalog.browse";

    /// <summary>The module name Identity seeds this catalog under.</summary>
    public const string ModuleName = "catalog";

    public static readonly IReadOnlyList<string> All = [Browse, Manage];
}
