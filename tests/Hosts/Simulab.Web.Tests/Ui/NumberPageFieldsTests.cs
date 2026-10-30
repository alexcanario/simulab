using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-181 AC12, `## Screens and API` of the item: each number field of the two catalog forms uses the control of its
/// kind. Read from the page source, like the text kinds (F-183); each form's own tests prove it still works.
/// </summary>
public sealed class NumberPageFieldsTests
{
    public static TheoryData<string, string, string> Fields => new()
    {
        { "Catalog/NoticeSubjectDialog.razor", "notice-subject-question-count", "AppIntegerField" },
        { "Catalog/NoticeSubjectDialog.razor", "notice-subject-points", "AppScoreField" },
        { "Catalog/ExamEditionForm.razor", "edition-objective-cut-off", "AppScoreField" },
        { "Catalog/ExamEditionForm.razor", "edition-essay-max-score", "AppScoreField" },
        { "Catalog/ExamEditionForm.razor", "edition-essay-cut-off", "AppScoreField" },
        { "Catalog/ExamEditionForm.razor", "edition-notice-year", "AppYearField" },
    };

    [Theory]
    [MemberData(nameof(Fields))]
    public void Field_UsesTheControlOfItsKind(string page, string id, string control)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        Regex.Matches(source, $"""<(App\w+)\b[^<>]*?\bId="{Regex.Escape(id)}" """.TrimEnd(), RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Should().Equal([control], because: $"{id} in {page} is a number (F-181)");
    }

    [Theory]
    [InlineData("Catalog/NoticeSubjectDialog.razor")]
    [InlineData("Catalog/ExamEditionForm.razor")]
    public void Form_HasNoNumberInputAndNoTextFieldHoldingANumber(string page)
    {
        var source = File.ReadAllText(Path.Combine(PagesRoot(), page.Replace('/', Path.DirectorySeparatorChar)));

        // Duration hours and minutes keep their number input until F-182 replaces them with AppDurationField.
        var withoutDuration = Regex.Replace(source, """<AppTextField[^<>]*Id="edition-duration-(hours|minutes)".*?/>""", string.Empty, RegexOptions.Singleline);

        withoutDuration.Should().NotContain("InputType.Number");
        Regex.Matches(withoutDuration, """<AppTextField[^<>]*TValue="string"[^<>]*Id="(notice-subject-(question-count|points)|edition-(objective-cut-off|essay-max-score|essay-cut-off))" """.TrimEnd(), RegexOptions.Singleline)
            .Should().BeEmpty();
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

/// <summary>F-181 AC13: every text of the number controls exists in pt-BR, pt-PT and en (rule: i18n).</summary>
public sealed class NumberResourcesTests
{
    private static readonly ResourceManager Manager =
        new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string> KeysByCulture()
    {
        string[] keys =
        [
            "Number.Invalid", "Number.InvalidInteger", "Number.Range", "Number.Unit.Points",
            "Number.Stepper.Increase", "Number.Stepper.Decrease", "Number.None",
            "Gallery.Section.Numbers", "Gallery.Numbers.Hint", "Gallery.Numbers.Integer", "Gallery.Numbers.Decimal",
            "Gallery.Numbers.Score", "Gallery.Numbers.Year", "Gallery.Numbers.Text",
            "Gallery.Numbers.State.Empty", "Gallery.Numbers.State.Filled", "Gallery.Numbers.State.Error",
            "Gallery.Numbers.State.Disabled", "Gallery.Numbers.State.ReadOnly", "Gallery.Numbers.State.LongLabel",
            "Gallery.Numbers.State.Stepper"
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
