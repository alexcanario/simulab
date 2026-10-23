namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-126 AC9 (BR5, BR11): the question form is the one routed page that may carry the amber action, and it carries
/// exactly one, in its compact variant. <see cref="ActionColourScopeTests"/> keeps the rule for every other page.
/// </summary>
public sealed class QuestionFormActionScopeTests
{
    [Fact]
    public void QuestionForm_HasExactlyOneCompactActionKeyAndNoTertiaryButton()
    {
        var form = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web", "Components", "Pages", "QuestionBank", "QuestionForm.razor"));

        var keys = ActionColourScopeTests.ActionShortcuts(form);
        keys.Should().ContainSingle("Publish is the screen's single amber action");
        keys[0].Should().Contain("Compact", "the aside is narrower than 360px");
        ActionColourScopeTests.TertiaryButtons(form).Should().BeEmpty("the amber action is the kit key, not a tertiary MudButton");
    }

    [Fact]
    public void OtherRoutedPages_StillHaveNoAmberActionAndTheScanNamesTheAllowedPage()
    {
        var offenders = Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web"), "*.razor", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(file => file.Replace('\\', '/'))
            .Where(file => !file.Contains("/Components/Pages/Dev/", StringComparison.Ordinal) && !file.Contains("/Components/Ui/", StringComparison.Ordinal))
            .Where(file => !file.EndsWith("/QuestionBank/QuestionForm.razor", StringComparison.Ordinal))
            .Where(file => !file.EndsWith("/Pages/Error.razor", StringComparison.Ordinal))
            .Where(file => ActionColourScopeTests.ActionShortcuts(File.ReadAllText(file)).Count > 0 || ActionColourScopeTests.TertiaryButtons(File.ReadAllText(file)).Count > 0)
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty("only QuestionForm.razor may use the amber action until another screen item (F-127 to F-134) picks its own");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return directory!.FullName;
    }
}
