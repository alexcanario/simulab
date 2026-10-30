using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-185 AC8, AC9: the period filters of both audit lists are the range field in its presets-only mode, and the
/// "When" cell shows the relative time. Read from the page source; each page's own tests prove it works.
/// </summary>
public sealed class DatePageFieldsTests
{
    public static TheoryData<string, string> Periods => new()
    {
        { "Admin/AccountEvents.razor", "account-events-period-filter" },
        { "Admin/RoleHistory.razor", "history-period-filter" },
    };

    [Theory]
    [MemberData(nameof(Periods))]
    public void Period_IsTheRangeFieldInPresetsOnlyMode(string page, string id)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        var match = Regex.Match(source, $"""<(App\w+)\b[^<>]*?\bId="{Regex.Escape(id)}"[^<>]*?/>""", RegexOptions.Singleline);

        match.Groups[1].Value.Should().Be("AppDateRangeField");
        match.Value.Should().Contain("ThisYear=\"false\"").And.Contain("AllowCustom=\"false\"");
    }

    [Theory]
    [MemberData(nameof(Periods))]
    public void WhenColumn_ShowsTheRelativeTime(string page, string _)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        source.Should().Contain("<AppRelativeTime");
    }

    private static string PagesRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web", "Components", "Pages");
    }
}
