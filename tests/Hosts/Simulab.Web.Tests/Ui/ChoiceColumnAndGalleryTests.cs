using System.Globalization;
using System.Linq.Expressions;
using System.Resources;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-184 AC6, BR3: the bool column shows an icon with the words.</summary>
public sealed class AppBoolColumnTests : KitTestContext
{
    private sealed record Row(int Number, bool? Done);

    [Fact]
    public void Column_ShowsTheWordsOfEachRow_WithAnIcon()
    {
        Render<MudPopoverProvider>();
        IReadOnlyList<Row> rows = [new(1, true), new(2, false), new(3, null)];

        var table = Render<AppDataTable<Row>>(parameters => parameters
            .Add(p => p.LoadAsync, (query, token) => Task.FromResult(new AppTablePage<Row>(rows, rows.Count)))
            .Add(p => p.EmptyActionText, "Add")
            .Add(p => p.OnEmptyAction, () => { })
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<AppBoolColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(AppBoolColumn<Row>.Property), (Expression<Func<Row, bool?>>)(x => x.Done));
                builder.AddComponentParameter(2, nameof(AppBoolColumn<Row>.Title), "Done");
                builder.CloseComponent();
            }));

        table.WaitForAssertion(() => table.FindAll("tbody td .app-bool-text").Should().HaveCount(3));
        table.FindAll("tbody td .app-bool-text").Select(cell => cell.TextContent.Trim()).Should().Equal("Yes", "No", "—");
        table.FindAll("tbody td .app-bool-text svg").Should().HaveCount(2, "a row with no value has no icon");
    }
}

/// <summary>F-184 AC8: the gallery shows the choice controls, each in the states of the shared contract.</summary>
public sealed class UiGalleryChoicesTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        return Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("Switch (AppSwitch)")]
    [InlineData("Segmented (AppSegmentedField)")]
    [InlineData("Yes / no (AppYesNoField)")]
    [InlineData("Level (AppLevelField)")]
    [InlineData("Level filter (AppLevelFilterField)")]
    [InlineData("Level and yes / no as text (AppLevelBadge, AppBoolText)")]
    [InlineData("Language (AppLanguageField)")]
    public void Render_Development_ChoicesSectionNamesEachKind(string kind)
    {
        RenderGallery().Find("#gallery-choices").TextContent.Should().Contain(kind);
    }

    [Theory]
    [InlineData("Empty")]
    [InlineData("Filled")]
    [InlineData("Error")]
    [InlineData("Disabled")]
    [InlineData("Read-only")]
    public void Render_Development_ChoicesSectionShowsEachState(string state)
    {
        RenderGallery().Find("#gallery-choices").TextContent.Should().Contain(state);
    }

    [Fact]
    public void Render_Development_ChoicesSectionUsesTheKitControls()
    {
        var section = RenderGallery().Find("#gallery-choices");

        section.QuerySelectorAll(".app-switch-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-segmented-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-yes-no-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-level-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-level-filter").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-level-badge").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-language-field").Should().NotBeEmpty();
    }
}

/// <summary>
/// F-184 AC9, AC10, AC11: each field of the screens uses the control of its kind. Read from the page source, like the
/// text and number kinds; each page's own tests prove it still works.
/// </summary>
public sealed class ChoicePageFieldsTests
{
    public static TheoryData<string, string, string> Fields => new()
    {
        { "QuestionBank/QuestionForm.razor", "question-difficulty", "AppLevelField" },
        { "QuestionBank/QuestionForm.razor", "question-true-false-key", "AppYesNoField" },
        { "QuestionBank/QuestionForm.razor", "question-content-language", "AppLanguageField" },
        { "QuestionBank/BaseTextForm.razor", "base-text-content-language", "AppLanguageField" },
        { "Catalog/ExamForm.razor", "exam-content-language", "AppLanguageField" },
        { "Identity/Account.razor", "account-preferred-language", "AppLanguageField" },
        { "QuestionBank/Questions.razor", "questions-filter-difficulty", "AppLevelFilterField" },
    };

    [Theory]
    [MemberData(nameof(Fields))]
    public void Field_UsesTheControlOfItsKind(string page, string id, string control)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        Regex.Matches(source, $"""<(App\w+)\b[^<>]*?\bId="{Regex.Escape(id)}" """.TrimEnd(), RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Should().Equal([control], because: $"{id} in {page} is a choice of that kind (F-184)");
    }

    [Fact]
    public void BaseTextsFilter_UsesTheLanguageField()
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), "QuestionBank", "BaseTexts.razor"));

        source.Should().Contain("<AppLanguageField").And.NotContain("LanguageOptions");
    }

    [Fact]
    public void QuestionsTable_ShowsTheDifficultyWithTheBadge()
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), "QuestionBank", "Questions.razor"));

        source.Should().Contain("<AppLevelBadge");
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

/// <summary>F-184 AC12: every text of the choice controls exists in pt-BR, pt-PT and en (rule: i18n).</summary>
public sealed class ChoiceResourcesTests
{
    private static readonly ResourceManager Manager =
        new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string> KeysByCulture()
    {
        string[] keys =
        [
            "Choice.Yes", "Choice.No", "Choice.NotSet", "Level.Clear", "Level.DotName",
            "Questions.Difficulty.Level.1", "Questions.Difficulty.Level.2", "Questions.Difficulty.Level.3",
            "Questions.Difficulty.Level.4", "Questions.Difficulty.Level.5",
            "Gallery.Section.Choices", "Gallery.Choices.Hint", "Gallery.Choices.Switch", "Gallery.Choices.Segmented",
            "Gallery.Choices.YesNo", "Gallery.Choices.Level", "Gallery.Choices.LevelFilter", "Gallery.Choices.Text",
            "Gallery.Choices.Language", "Gallery.Choices.State.Empty", "Gallery.Choices.State.Filled",
            "Gallery.Choices.State.Error", "Gallery.Choices.State.Disabled", "Gallery.Choices.State.ReadOnly",
            "Gallery.Choices.Label.Reminders", "Gallery.Choices.Label.Period", "Gallery.Choices.Label.Key",
            "Gallery.Choices.Label.Difficulty", "Gallery.Choices.Label.Language", "Gallery.Choices.Option.Day",
            "Gallery.Choices.Option.Week", "Gallery.Choices.Option.Month"
        ];
        var data = new TheoryData<string, string>();
        foreach (var culture in new[] { "en", "pt-BR", "pt-PT" })
        {
            foreach (var key in keys)
            {
                data.Add(culture, key);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(KeysByCulture))]
    public void Key_HasATextInTheLanguage(string culture, string key)
    {
        var text = Manager.GetString(key, new CultureInfo(culture));

        text.Should().NotBeNullOrWhiteSpace($"'{key}' must exist in {culture}");
    }
}
