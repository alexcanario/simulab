using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Catalog.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-168 AC14: every text of the exam rules exists in en, pt-BR and pt-PT and keeps its placeholders in all three.
/// ResourceParityTests checks that no key is missing in a language and ErrorCodeTextTests that every code has a text;
/// this pins what the item needs.
/// </summary>
public sealed partial class ExamRulesResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "ExamRules.Section.Title", "ExamRules.Section.Subtitle",
        "ExamRules.Field.Duration", "ExamRules.Field.DurationHours", "ExamRules.Field.DurationMinutes", "ExamRules.Field.Duration.Hint",
        "ExamRules.Field.ObjectiveCutOff", "ExamRules.Field.ObjectiveCutOff.Hint",
        "ExamRules.Field.EssayMaxScore", "ExamRules.Field.EssayCutOff", "ExamRules.Field.EssayCutOff.Hint",
        "ExamRules.Points.Suffix",
        "ExamRules.ObjectiveTotal", "ExamRules.ObjectiveTotal.Unknown", "ExamRules.ObjectiveTotal.NoSubjects",
        "ExamRules.Missing.Title", "ExamRules.Missing.Duration", "ExamRules.Missing.ObjectiveCutOff",
        "ExamRules.Missing.EssayMaxScore", "ExamRules.Missing.Subjects",
        "ExamRules.CutOffAboveTotal",
        "ExamRules.Aside.Title", "ExamRules.Aside.Duration", "ExamRules.Aside.CutOff", "ExamRules.Aside.CutOffOfTotal",
        "ExamRules.Duration.Hours", "ExamRules.Duration.HoursMinutes", "ExamRules.Duration.Minutes",
        "NoticeSubjects.Field.PointsPerQuestion", "NoticeSubjects.Field.PointsPerQuestion.Hint",
        "NoticeSubjects.Row.Points", "NoticeSubjects.Total.Points",
        "NoticeSubjects.Total.NoPoints.One", "NoticeSubjects.Total.NoPoints.Many",
        "CatalogExam.Rules.Objective", "CatalogExam.Rules.ObjectiveWithCutOff", "CatalogExam.Rules.CutOffOnly",
        "CatalogExam.Rules.Essay", "CatalogExam.Rules.EssayWithCutOff",
        CatalogErrorCodes.ExamEditionDurationInvalid, CatalogErrorCodes.ExamEditionObjectiveCutOffInvalid,
        CatalogErrorCodes.ExamEditionEssayMaxScoreInvalid, CatalogErrorCodes.ExamEditionEssayCutOffInvalid,
        CatalogErrorCodes.ExamEditionEssayCutOffWithoutMax, CatalogErrorCodes.ExamEditionEssayCutOffAboveMax,
        CatalogErrorCodes.NoticeSubjectPointsPerQuestionInvalid
    ]);

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_ExistsInTheThreeLanguages(string key)
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace($"'{key}' in {culture}");
        }
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_KeepsEveryPlaceholderInEveryLanguage(string key)
    {
        var english = Manager.GetString(key, new CultureInfo("en"));
        english.Should().NotBeNull($"'{key}' in en");
        var expected = Placeholders(english!);

        foreach (var culture in Cultures)
        {
            Placeholders(Manager.GetString(key, new CultureInfo(culture)) ?? string.Empty).Should().Equal(expected, $"'{key}' in {culture}");
        }
    }

    private static string[] Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
