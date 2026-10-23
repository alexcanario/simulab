using Microsoft.AspNetCore.Components.Routing;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Layout;

/// <summary>The menu registry: the only place that lists menu items.</summary>
public static class NavigationItems
{
    public static readonly IReadOnlyList<NavigationItem> All =
    [
        new(null, "/", AppIcons.Home, "Nav.Home", NavLinkMatch.All),
        new(NavigationSection.Study, "/catalog", AppIcons.Catalog, "Nav.Catalog", RequiredPermission: CatalogPermissions.Browse),
        new(NavigationSection.Content, "/admin/organizers", AppIcons.Organizers, "Nav.Organizers", RequiredPermission: CatalogPermissions.Manage),
        new(NavigationSection.Content, "/admin/issuing-authorities", AppIcons.IssuingAuthorities, "Nav.IssuingAuthorities", RequiredPermission: CatalogPermissions.Manage),
        new(NavigationSection.Content, "/admin/exams", AppIcons.Exams, "Nav.Exams", RequiredPermission: CatalogPermissions.Manage),
        new(NavigationSection.Content, "/admin/subjects", AppIcons.Subjects, "Nav.Subjects", RequiredPermission: CatalogPermissions.Manage),
        new(NavigationSection.Content, "/admin/questions", AppIcons.Questions, "Nav.Questions", RequiredPermission: QuestionBankPermissions.Manage),
        // F-105 (BR10): the review queue follows the questions and is behind its own permission, so a role holding
        // only questions.review sees this item and nothing else of the question bank.
        new(NavigationSection.Content, "/admin/questions/review", AppIcons.Review, "Nav.ReviewQueue", RequiredPermission: QuestionBankPermissions.Review),
        new(NavigationSection.Content, "/admin/base-texts", AppIcons.BaseText, "Nav.BaseTexts", RequiredPermission: QuestionBankPermissions.Manage),
        new(NavigationSection.Administration, "/admin/roles", AppIcons.Roles, "Nav.Roles", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/users", AppIcons.Users, "Nav.Users", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/role-history", AppIcons.History, "Nav.RoleHistory", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Administration, "/admin/account-events", AppIcons.Security, "Nav.AccountEvents", RequiredPermission: IdentityPermissions.RolesManage),
        new(NavigationSection.Development, "/dev/ui", AppIcons.Build, "Nav.Dev.UiKit", DevelopmentOnly: true),
        new(NavigationSection.Development, "/dev/ai", AppIcons.Ai, "Nav.Dev.Ai", DevelopmentOnly: true),
    ];

    /// <summary>F-134: the icon that stands for a section on the home page.</summary>
    public static string SectionIcon(NavigationSection section) => section switch
    {
        NavigationSection.Study => AppIcons.SectionStudy,
        NavigationSection.Content => AppIcons.SectionContent,
        NavigationSection.Administration => AppIcons.SectionAdministration,
        NavigationSection.Development => AppIcons.SectionDevelopment,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "A new menu section needs an icon here.")
    };

    /// <summary>Items the current user may see (F-6, BR6): a permission-gated item needs <paramref name="hasPermission"/> to say yes.</summary>
    public static IReadOnlyList<NavigationItem> Visible(IEnumerable<NavigationItem> items, bool isDevelopment, Func<string, bool> hasPermission) =>
        [.. items.Where(item =>
            (item.RequiredPermission is null || hasPermission(item.RequiredPermission))
            && (isDevelopment || !item.DevelopmentOnly))];

    /// <summary>Visible sectioned items grouped by section, in section order; empty sections are left out.</summary>
    public static IReadOnlyList<IGrouping<NavigationSection, NavigationItem>> Sections(IEnumerable<NavigationItem> visible) =>
        [.. visible
            .Where(item => item.Section is not null)
            .GroupBy(item => item.Section!.Value)
            .OrderBy(group => group.Key)];
}
