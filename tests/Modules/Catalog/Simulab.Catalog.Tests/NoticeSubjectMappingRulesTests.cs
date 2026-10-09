using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-75 BR1, BR4, BR5: the rules of a mapping that need no database. The overlap rule is judged against the
/// saved mapping, so an overlap a topic move created never blocks the save of the row (AC6).
/// </summary>
public sealed class NoticeSubjectMappingRulesTests
{
    private static readonly Guid Math = Guid.CreateVersion7();
    private static readonly Guid Logic = Guid.CreateVersion7();
    private static readonly Guid Fractions = Guid.CreateVersion7();
    private static readonly Guid Propositions = Guid.CreateVersion7();

    private static readonly Dictionary<Guid, Guid> TopicSubjects = new()
    {
        [Fractions] = Math,
        [Propositions] = Logic
    };

    private static NoticeSubjectMappingRequest Whole(Guid id) => new(SubjectId: id);

    private static NoticeSubjectMappingRequest Topic(Guid id) => new(TopicId: id);

    private static MappingEntry WholeEntry(Guid id) => new(id, null);

    private static MappingEntry TopicEntry(Guid id) => new(null, id);

    // AC2
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Normalize_EntryWithBothOrNeitherId_IsInvalid(bool subject, bool topic)
    {
        var entry = new NoticeSubjectMappingRequest(subject ? Math : null, topic ? Fractions : null);

        var result = NoticeSubjectMappingRules.Normalize([entry]);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingInvalid);
    }

    [Fact]
    public void Normalize_EmptyGuid_IsInvalid()
    {
        var result = NoticeSubjectMappingRules.Normalize([Whole(Guid.Empty)]);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingInvalid);
    }

    [Fact]
    public void Normalize_NullOrEmpty_IsNotMapped()
    {
        NoticeSubjectMappingRules.Normalize(null).Value.Should().BeEmpty();
        NoticeSubjectMappingRules.Normalize([]).Value.Should().BeEmpty();
    }

    // AC7
    [Fact]
    public void Normalize_SameTopicTwice_IsKeptOnce()
    {
        var result = NoticeSubjectMappingRules.Normalize([Topic(Fractions), Whole(Logic), Topic(Fractions)]);

        result.Value.Should().Equal(TopicEntry(Fractions), WholeEntry(Logic));
    }

    // AC3: the limit counts distinct entries.
    [Fact]
    public void Normalize_FiftyOneDistinctEntries_IsTooMany()
    {
        var entries = Enumerable.Range(0, CatalogLimits.NoticeSubjectMappingMax + 1)
            .Select(_ => Whole(Guid.CreateVersion7()))
            .ToList();

        var result = NoticeSubjectMappingRules.Normalize(entries);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingTooMany);
    }

    [Fact]
    public void Normalize_FiftyDistinctEntriesWithRepeats_IsAccepted()
    {
        var distinct = Enumerable.Range(0, CatalogLimits.NoticeSubjectMappingMax).Select(_ => Whole(Guid.CreateVersion7())).ToList();

        var result = NoticeSubjectMappingRules.Normalize([.. distinct, .. distinct.Take(10)]);

        result.Value.Should().HaveCount(CatalogLimits.NoticeSubjectMappingMax);
    }

    [Fact]
    public void Normalize_ARequestFarOverTheLimit_IsRefusedWithoutBeingRead()
    {
        var one = Whole(Math);
        var entries = Enumerable.Repeat(one, CatalogLimits.NoticeSubjectMappingMax * 2 + 1).ToList();

        var result = NoticeSubjectMappingRules.Normalize(entries);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingTooMany);
    }

    // AC5
    [Fact]
    public void CheckOverlap_WholeSubjectAndOneOfItsTopics_IsRefused()
    {
        var result = NoticeSubjectMappingRules.CheckOverlap([WholeEntry(Math), TopicEntry(Fractions)], TopicSubjects, []);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingOverlap);
    }

    [Fact]
    public void CheckOverlap_WholeSubjectAndATopicOfAnotherSubject_IsAccepted()
    {
        var result = NoticeSubjectMappingRules.CheckOverlap([WholeEntry(Math), TopicEntry(Propositions)], TopicSubjects, []);

        result.IsSuccess.Should().BeTrue();
    }

    // AC6, BR4: the overlap was already saved (a topic moved under a subject mapped whole), so saving the row
    // again is accepted; it is the new overlap that is refused.
    [Fact]
    public void CheckOverlap_AnOverlapTheSavedMappingAlreadyHeld_IsKept()
    {
        var saved = new[] { WholeEntry(Math), TopicEntry(Fractions) };

        var result = NoticeSubjectMappingRules.CheckOverlap(saved, TopicSubjects, saved);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckOverlap_AddingAnotherTopicToAnExistingOverlap_IsRefused()
    {
        var other = Guid.CreateVersion7();
        var topics = new Dictionary<Guid, Guid>(TopicSubjects) { [other] = Math };
        var saved = new[] { WholeEntry(Math), TopicEntry(Fractions) };

        var result = NoticeSubjectMappingRules.CheckOverlap([.. saved, TopicEntry(other)], topics, saved);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingOverlap);
    }

    [Fact]
    public void CheckOverlap_TheTopicWasSavedButTheWholeSubjectIsNew_IsRefused()
    {
        var result = NoticeSubjectMappingRules.CheckOverlap(
            [WholeEntry(Math), TopicEntry(Fractions)],
            TopicSubjects,
            [TopicEntry(Fractions)]);

        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectMappingOverlap);
    }
}
