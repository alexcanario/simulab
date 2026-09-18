using Microsoft.AspNetCore.Components.Routing;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Layout;

public class NavigationItemsTests
{
    private static bool NoPermissions(string permission) => false;

    [Fact]
    public void Visible_Development_ShowsHomeAndUiKit()
    {
        var visible = NavigationItems.Visible(NavigationItems.All, isDevelopment: true, NoPermissions);

        visible.Select(i => i.Route).Should().Equal("/", "/dev/ui");
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
