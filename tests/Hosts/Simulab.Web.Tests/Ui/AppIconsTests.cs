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

    // F-95: the question bank's screens and the rich text field's toolbar.
    [Fact]
    public void AppIcons_QuestionBankAndRichTextToolbar_AreTheOutlinedIconsTheItemNames()
    {
        AppIcons.Questions.Should().Be(Icons.Material.Outlined.Quiz);
        AppIcons.FormatBold.Should().Be(Icons.Material.Outlined.FormatBold);
        AppIcons.FormatItalic.Should().Be(Icons.Material.Outlined.FormatItalic);
        AppIcons.FormatUnderline.Should().Be(Icons.Material.Outlined.FormatUnderlined);
        AppIcons.FormatStrikethrough.Should().Be(Icons.Material.Outlined.FormatStrikethrough);
        AppIcons.Superscript.Should().Be(Icons.Material.Outlined.Superscript);
        AppIcons.Subscript.Should().Be(Icons.Material.Outlined.Subscript);
        AppIcons.BulletList.Should().Be(Icons.Material.Outlined.FormatListBulleted);
        AppIcons.NumberedList.Should().Be(Icons.Material.Outlined.FormatListNumbered);
        AppIcons.Quote.Should().Be(Icons.Material.Outlined.FormatQuote);
        AppIcons.ClearFormatting.Should().Be(Icons.Material.Outlined.FormatClear);
        AppIcons.Link.Should().Be(Icons.Material.Outlined.Link, "the link button reuses the existing constant");
    }

    // F-169: the start action of an edition, the answer sheet and the check of an answered cell.
    [Fact]
    public void AppIcons_ExamSimulator_AreTheOutlinedIconsTheItemNames()
    {
        AppIcons.StartAttempt.Should().Be(Icons.Material.Outlined.PlayCircle);
        AppIcons.AnswerSheet.Should().Be(Icons.Material.Outlined.GridView);
        AppIcons.Check.Should().Be(Icons.Material.Outlined.Check);
    }

    [Fact]
    public void AppIcons_SemanticNames_CoverTheKitActions()
    {
        Constants(typeof(AppIcons)).Select(f => f.Name).Should().Contain(
            ["Add", "Edit", "Delete", "Search", "More", "Retry", "Close", "Back", "Save", "Warning", "Error", "Empty", "LightMode", "DarkMode"]);
    }
}
