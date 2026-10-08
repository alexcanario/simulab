using System.Reflection;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppIconsTests
{
    private static IEnumerable<FieldInfo> Constants(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral);

    [Fact]
    public void AppIcons_Every_IsMaterialOutlined()
    {
        var outlined = Constants(typeof(Icons.Material.Outlined)).Select(f => (string)f.GetRawConstantValue()!).ToHashSet();
        var icons = Constants(typeof(AppIcons)).ToList();

        icons.Should().NotBeEmpty();
        icons.Should().AllSatisfy(icon => outlined.Should().Contain((string)icon.GetRawConstantValue()!, $"AppIcons.{icon.Name} must be Material Outlined"));
    }

    // F-35: the edition section, the notice, its link and the date field's calendar button.
    [Fact]
    public void AppIcons_EditionsNoticeLinkAndCalendar_AreTheOutlinedIconsTheItemNames()
    {
        AppIcons.Editions.Should().Be(Icons.Material.Outlined.EventNote);
        AppIcons.Notice.Should().Be(Icons.Material.Outlined.Article);
        AppIcons.Link.Should().Be(Icons.Material.Outlined.Link);
        AppIcons.Calendar.Should().Be(Icons.Material.Outlined.CalendarToday);
    }

    // F-36: the student catalog, clearing filters and a link that opens a new tab.
    [Fact]
    public void AppIcons_CatalogClearFiltersAndOpenInNew_AreTheOutlinedIconsTheItemNames()
    {
        AppIcons.Catalog.Should().Be(Icons.Material.Outlined.ManageSearch);
        AppIcons.ClearFilters.Should().Be(Icons.Material.Outlined.FilterAltOff);
        AppIcons.OpenInNew.Should().Be(Icons.Material.Outlined.OpenInNew);
    }

    // F-74: the notice subjects section and the move pair of a sortable list.
    [Fact]
    public void AppIcons_NoticeSubjectsAndMovePair_AreTheOutlinedIconsTheItemNames()
    {
        AppIcons.NoticeSubjects.Should().Be(Icons.Material.Outlined.ListAlt);
        AppIcons.MoveUp.Should().Be(Icons.Material.Outlined.ArrowUpward);
        AppIcons.MoveDown.Should().Be(Icons.Material.Outlined.ArrowDownward);
    }

    [Fact]
    public void AppIcons_SemanticNames_CoverTheKitActions()
    {
        Constants(typeof(AppIcons)).Select(f => f.Name).Should().Contain(
            ["Add", "Edit", "Delete", "Search", "More", "Retry", "Close", "Back", "Save", "Warning", "Error", "Empty", "LightMode", "DarkMode"]);
    }
}
