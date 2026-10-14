using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-75 AC16: every text of the mapping screens exists in en, pt-BR and pt-PT and keeps its placeholders in all three;
/// pt-PT says "aviso" where pt-BR says "edital"; the "›" lives inside a resource format so a language can change it.
/// ResourceParityTests checks that no key is missing in a language; this pins what these screens need.
/// </summary>
public sealed partial class NoticeSubjectMappingResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "Common.RemoveItem",
        "NoticeSubjects.Field.Covers", "NoticeSubjects.Field.Covers.Placeholder", "NoticeSubjects.Field.Covers.Hint",
        "NoticeSubjects.Field.Covers.Options", "NoticeSubjects.Field.Covers.Picked", "NoticeSubjects.Field.Covers.Count",
        "NoticeSubjects.Field.Covers.None", "NoticeSubjects.Field.Covers.Loading", "NoticeSubjects.Field.Covers.LoadFailed",
        "NoticeSubjects.Field.Covers.EmptyTaxonomy", "NoticeSubjects.Field.Covers.LimitReached",
        "NoticeSubjects.Covers.WholeMarker", "NoticeSubjects.Covers.Topic", "NoticeSubjects.Covers.Topic.Spoken",
        "NoticeSubjects.Covers.CoveredByWhole", "NoticeSubjects.Covers.WholeBlocked", "NoticeSubjects.Covers.Added",
        "NoticeSubjects.Covers.Removed", "NoticeSubjects.Covers.Cleared", "NoticeSubjects.Covers.Overlaps",
        "NoticeSubjects.Covers.Gone", "NoticeSubjects.Row.NotMapped",
        "NoticeSubjects.Total.Unmapped.One", "NoticeSubjects.Total.Unmapped.Many",
        "Subjects.Delete.DisabledInUse", "Topics.Delete.DisabledInUse",
        // The error codes are not listed here: ErrorCodeTextTests finds every code of the solution and checks its text.
        "Gallery.Section.Chip", "Gallery.Chip.Hint", "Gallery.Section.MultiPickField", "Gallery.MultiPickField.Hint",
        "Gallery.MultiPickField.Label"
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
    public void ConcurrentSaveRefusal_SaysWhatTheOwnerApproved()
    {
        Manager.GetString("notice_subject.mapping_conflict", new CultureInfo("pt-BR")).Should()
            .Be("Outra pessoa salvou esta disciplina ao mesmo tempo. Salve de novo.");
        Manager.GetString("notice_subject.mapping_conflict", new CultureInfo("pt-PT")).Should()
            .Be("Outra pessoa guardou esta disciplina ao mesmo tempo. Guarde novamente.");
        Manager.GetString("notice_subject.mapping_conflict", new CultureInfo("en")).Should()
            .Be("Someone else saved this notice subject at the same time. Save again.");
    }

    [Fact]
    public void Covers_PtBrSaysEditalAndPtPtSaysAviso()
    {
        Manager.GetString("NoticeSubjects.Field.Covers.Hint", new CultureInfo("pt-BR")).Should().Contain("disciplina do edital");
        Manager.GetString("NoticeSubjects.Field.Covers.Hint", new CultureInfo("pt-PT")).Should().Contain("disciplina do aviso");
        Manager.GetString("notice_subject.mapping_too_many", new CultureInfo("pt-BR")).Should().Contain("edital");
        Manager.GetString("notice_subject.mapping_too_many", new CultureInfo("pt-PT")).Should().Contain("aviso");
    }

    // The "›" is part of the format, not of the code, so a language can change it; the subject comes first, then the topic.
    [Fact]
    public void TopicForms_TheSeparatorLivesInsideTheResourceAndTheSpokenFormNamesTheTopicFirst()
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString("NoticeSubjects.Covers.Topic", new CultureInfo(culture)).Should().Be("{0} › {1}");
        }

        Manager.GetString("NoticeSubjects.Covers.Topic.Spoken", new CultureInfo("en")).Should().Be("{1}, topic of {0}");
        Manager.GetString("NoticeSubjects.Covers.Topic.Spoken", new CultureInfo("pt-BR")).Should().Be("{1}, tópico de {0}");
        Manager.GetString("NoticeSubjects.Covers.Topic.Spoken", new CultureInfo("pt-PT")).Should().Be("{1}, tópico de {0}");
    }

    private static IReadOnlyList<string> Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
