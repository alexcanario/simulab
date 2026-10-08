using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Catalog.Contracts;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-74 AC20: every text of the notice subjects screen exists in en, pt-BR and pt-PT, keeps its placeholders in all
/// three, and pt-PT says "aviso" where pt-BR says "edital" (owner, screen question 4). ResourceParityTests checks that
/// no key is missing in a language; this pins what this screen needs.
/// </summary>
public sealed partial class NoticeSubjectResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "Common.MoveUp", "Common.MoveDown",
        "NoticeSubjects.Section.Title", "NoticeSubjects.Section.Subtitle", "NoticeSubjects.Section.SaveEditionFirst",
        "NoticeSubjects.Empty", "NoticeSubjects.Group.None",
        "NoticeSubjects.Row.Questions.One", "NoticeSubjects.Row.Questions.Many", "NoticeSubjects.Row.NoCount.Spoken",
        "NoticeSubjects.MoveUp.DisabledFirst", "NoticeSubjects.MoveDown.DisabledLast",
        "NoticeSubjects.Moved", "NoticeSubjects.Moved.NoGroup",
        "NoticeSubjects.Total.One", "NoticeSubjects.Total.Many", "NoticeSubjects.Total.None",
        "NoticeSubjects.Total.Unstated.One", "NoticeSubjects.Total.Unstated.Many",
        "NoticeSubjects.Form.AddTitle", "NoticeSubjects.Form.EditTitle",
        "NoticeSubjects.Field.Group", "NoticeSubjects.Field.Group.Placeholder", "NoticeSubjects.Field.Group.Hint",
        "NoticeSubjects.Field.Group.MoveNote", "NoticeSubjects.Field.Group.Suggestions",
        "NoticeSubjects.Field.Label", "NoticeSubjects.Field.Label.Placeholder", "NoticeSubjects.Field.Label.Hint",
        "NoticeSubjects.Field.QuestionCount", "NoticeSubjects.Field.QuestionCount.Placeholder", "NoticeSubjects.Field.QuestionCount.Hint",
        "NoticeSubjects.Saved", "NoticeSubjects.Deleted", "NoticeSubjects.Delete.Object", "NoticeSubjects.Delete.Message",
        "ExamEditions.Delete.Message",
        CatalogErrorCodes.NoticeSubjectNotFound, CatalogErrorCodes.NoticeSubjectLabelRequired,
        CatalogErrorCodes.NoticeSubjectLabelTooShort, CatalogErrorCodes.NoticeSubjectLabelTooLong,
        CatalogErrorCodes.NoticeSubjectGroupTooLong, CatalogErrorCodes.NoticeSubjectQuestionCountInvalid,
        CatalogErrorCodes.NoticeSubjectDuplicate, CatalogErrorCodes.NoticeSubjectMoveInvalid,
        "Gallery.Section.SuggestField", "Gallery.SuggestField.Hint"
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
        var expected = Placeholders(Manager.GetString(key, new CultureInfo("en"))!);

        foreach (var culture in Cultures)
        {
            Placeholders(Manager.GetString(key, new CultureInfo(culture))!).Should().Equal(expected, $"'{key}' in {culture}");
        }
    }

    [Fact]
    public void Section_PtBrSaysEditalAndPtPtSaysAviso()
    {
        Manager.GetString("NoticeSubjects.Section.Title", new CultureInfo("en")).Should().Be("Notice subjects");
        Manager.GetString("NoticeSubjects.Section.Title", new CultureInfo("pt-BR")).Should().Be("Disciplinas do edital");
        Manager.GetString("NoticeSubjects.Section.Title", new CultureInfo("pt-PT")).Should().Be("Disciplinas do aviso");
    }

    // BR10: the edition's delete confirmation says its notice subjects leave with it, in every language.
    [Fact]
    public void EditionDeleteMessage_SaysTheNoticeSubjectsLeaveWithTheEdition()
    {
        Manager.GetString("ExamEditions.Delete.Message", new CultureInfo("en")).Should().Contain("with its notice subjects");
        Manager.GetString("ExamEditions.Delete.Message", new CultureInfo("pt-BR")).Should().Contain("com as disciplinas do edital dela");
        Manager.GetString("ExamEditions.Delete.Message", new CultureInfo("pt-PT")).Should().Contain("com as disciplinas do aviso");
    }

    [Theory]
    [InlineData("NoticeSubjects.Moved", "{0}", "{1}", "{2}", "{3}")]
    [InlineData("NoticeSubjects.Moved.NoGroup", "{0}", "{1}", "{2}")]
    public void MovedAnnouncement_TakesTheSubjectThePositionTheCountAndTheGroup(string key, params string[] placeholders)
    {
        Placeholders(Manager.GetString(key, new CultureInfo("en"))!).Should().Equal(placeholders);
    }

    private static IReadOnlyList<string> Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
