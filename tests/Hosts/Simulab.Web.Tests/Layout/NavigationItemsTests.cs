using Microsoft.AspNetCore.Components.Routing;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Layout;

public class NavigationItemsTests
{
    private static bool NoPermissions(string permission) => false;

    [Fact]
    public void Visible_Development_ShowsHomeAndTheDevelopmentPages()
    {
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: true, NoPermissions);

        visible.Select(i => i.Route).Should().Equal("/", "/dev/ui", "/dev/ai");
        NavigationItems.Sections(visible).Select(s => s.Key).Should().Equal(NavigationSection.Development);
    }

    [Fact]
    public void Visible_Production_ShowsOnlyHome()
    {
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, NoPermissions);

        visible.Select(i => i.Route).Should().Equal("/");
        NavigationItems.Sections(visible).Should().BeEmpty();
    }

    [Fact]
    public void Visible_ItemWithPermission_IsHiddenWithoutIt()
    {
        var items = new[]
        {
            new NavigationItem(NavigationSection.Administration, "/admin/roles", AppIcons.Edit, "Nav.Home", RequiredPermission: "roles.manage"),
            new NavigationItem(NavigationSection.Study, "/study", AppIcons.Edit, "Nav.Home"),
        };

        var visible = NavigationItems.Visible(items, isDevelopment: true, NoPermissions);

        visible.Select(i => i.Route).Should().Equal("/study");
        NavigationItems.Sections(visible).Select(s => s.Key).Should().Equal(NavigationSection.Study);
    }

    [Fact]
    public void Visible_ItemWithPermission_IsShownWhenGranted()
    {
        var items = new[]
        {
            new NavigationItem(NavigationSection.Administration, "/admin/roles", AppIcons.Edit, "Nav.Home", RequiredPermission: "roles.manage"),
            new NavigationItem(NavigationSection.Study, "/study", AppIcons.Edit, "Nav.Home"),
        };

        var visible = NavigationItems.Visible(items, isDevelopment: true, permission => permission == "roles.manage");

        visible.Select(i => i.Route).Should().Equal("/admin/roles", "/study");
    }

    [Fact]
    public void All_AdministrationItems_AreRolesUsersAndRoleHistoryBehindRolesManage()
    {
        // F-9, BR10; F-14, BR7; F-21, BR11: the back office screens share the one permission that gates them in the Api.
        NavigationItems.All.Where(item => item.Section == NavigationSection.Administration)
            .Select(item => (item.Route, item.RequiredPermission))
            .Should().Equal(
                ("/admin/roles", IdentityPermissions.RolesManage),
                ("/admin/users", IdentityPermissions.RolesManage),
                ("/admin/role-history", IdentityPermissions.RolesManage),
                ("/admin/account-events", IdentityPermissions.RolesManage));
    }

    [Fact]
    public void All_ContentItems_AreTheCatalogScreensBehindCatalogManage()
    {
        // F-33, BR4; F-34: the Content section holds the catalog screens, each behind the one permission of
        // its module. Only the exam list is in the menu; its form page is reached from the list.
        NavigationItems.All.Where(item => item.Section == NavigationSection.Content)
            .Select(item => (item.Route, item.RequiredPermission))
            .Should().Equal(
                ("/admin/organizers", CatalogPermissions.Manage),
                ("/admin/issuing-authorities", CatalogPermissions.Manage),
                ("/admin/exams", CatalogPermissions.Manage),
                ("/admin/subjects", CatalogPermissions.Manage));
    }

    [Fact]
    public void All_StudyItems_AreTheCatalogBehindCatalogBrowse()
    {
        // F-36, BR10: the student catalog is the Study section's first item, behind its own permission.
        NavigationItems.All.Where(item => item.Section == NavigationSection.Study)
            .Select(item => (item.Route, item.ResourceKey, item.Icon, item.RequiredPermission))
            .Should().Equal(("/catalog", "Nav.Catalog", AppIcons.Catalog, CatalogPermissions.Browse));
    }

    [Fact]
    public void Visible_WithCatalogBrowse_ShowsCatalogInTheStudySection()
    {
        // F-36, AC12: with the permission the item shows in the Study section.
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, permission => permission == CatalogPermissions.Browse);

        visible.Select(item => item.Route).Should().Equal("/", "/catalog");
        NavigationItems.Sections(visible).Select(section => section.Key).Should().Equal(NavigationSection.Study);
    }

    [Fact]
    public void Visible_WithCatalogManageButNotBrowse_HidesTheCatalogAndTheStudySection()
    {
        // F-36, AC12 and BR10: having catalog.manage alone does not open the student catalog.
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, permission => permission == CatalogPermissions.Manage);

        visible.Select(item => item.Route).Should().NotContain("/catalog");
        NavigationItems.Sections(visible).Select(section => section.Key).Should().NotContain(NavigationSection.Study);
    }

    [Fact]
    public void IsActive_CatalogItem_StaysCurrentOnTheExamPage()
    {
        // F-36: the prefix match keeps "Catalog" current on /catalog/exams/{id}.
        var catalog = NavigationItems.All.Single(item => item.Route == "/catalog");

        catalog.IsActive("catalog").Should().BeTrue();
        catalog.IsActive($"catalog/exams/{Guid.NewGuid()}?scope=State").Should().BeTrue();
        catalog.IsActive("catalogs").Should().BeFalse();
    }

    [Fact]
    public void Visible_WithoutCatalogManage_HidesTheContentSection()
    {
        // F-33, AC4: a Student has no catalog permission, so the section never appears in their menu.
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: false, NoPermissions);

        NavigationItems.Sections(visible).Select(section => section.Key).Should().NotContain(NavigationSection.Content);
    }

    [Fact]
    public void Sections_Unordered_ComeInSectionOrder()
    {
        var items = new[]
        {
            new NavigationItem(NavigationSection.Development, "/d", AppIcons.Edit, "k"),
            new NavigationItem(NavigationSection.Study, "/s", AppIcons.Edit, "k"),
            new NavigationItem(NavigationSection.Content, "/c", AppIcons.Edit, "k"),
        };

        NavigationItems.Sections(items).Select(s => s.Key).Should().Equal(
            NavigationSection.Study, NavigationSection.Content, NavigationSection.Development);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("/", true)]
    [InlineData("?x=1", true)]
    [InlineData("dev/ui", false)]
    public void IsActive_Home_MatchesOnlyRoot(string path, bool expected)
    {
        var home = new NavigationItem(null, "/", AppIcons.Home, "Nav.Home", NavLinkMatch.All);

        home.IsActive(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("dev/ui", true)]
    [InlineData("dev/ui/tables", true)]
    [InlineData("DEV/UI?tab=1", true)]
    [InlineData("dev/uikit", false)]
    [InlineData("", false)]
    public void IsActive_OtherItem_MatchesRoutePrefix(string path, bool expected)
    {
        var uiKit = new NavigationItem(NavigationSection.Development, "/dev/ui", AppIcons.Build, "Nav.Dev.UiKit");

        uiKit.IsActive(path).Should().Be(expected);
    }

    [Fact]
    public void All_Items_UseResourceKeysAndAbsoluteRoutes()
    {
        NavigationItems.All.Should().AllSatisfy(item =>
        {
            item.Route.Should().StartWith("/");
            item.ResourceKey.Should().StartWith("Nav.");
        });
    }
}
