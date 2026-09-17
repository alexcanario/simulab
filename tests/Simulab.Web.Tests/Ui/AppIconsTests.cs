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

    [Fact]
    public void AppIcons_SemanticNames_CoverTheKitActions()
    {
        Constants(typeof(AppIcons)).Select(f => f.Name).Should().Contain(
            ["Add", "Edit", "Delete", "Search", "More", "Retry", "Close", "Back", "Save", "Warning", "Error", "Empty", "LightMode", "DarkMode"]);
    }
}
