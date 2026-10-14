using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-100 BR8: the address of `/admin/questions` holds the search, the filters and the page, read without ever
/// failing and written without any key at its default (AC9, AC10).
/// </summary>
public sealed class QuestionListQueryStringTests
{
    private static readonly Guid TopicA = Guid.Parse("0198f5a0-0000-7000-8000-0000000000b1");
    private static readonly Guid TopicB = Guid.Parse("0198f5a0-0000-7000-8000-0000000000b2");
    private static readonly Guid Subject = Guid.Parse("0198f5a0-0000-7000-8000-0000000000a1");
    private static readonly Guid Organizer = Guid.Parse("0198f5a0-0000-7000-8000-0000000000c1");
    private static readonly Guid Edition = Guid.Parse("0198f5a0-0000-7000-8000-0000000000e1");

    [Fact]
    public void Parse_NoKeys_IsThePlainList()
    {
        var state = QuestionListQueryString.Parse("https://localhost/admin/questions");

        state.Should().Be(new QuestionListState());
        state.HasFilters.Should().BeFalse();
    }

    [Fact]
    public void Parse_EveryKey_IsRead_AndARepeatedKeyKeepsEveryValue()
    {
        var uri = $"https://localhost/admin/questions?search=cr%C3%A1se&status=Draft&status=published&type=TrueFalse"
            + $"&difficulty=2&difficulty=none&difficulty=5&subject={Subject}&topic={TopicA}&topic={TopicB}"
            + $"&organizer={Organizer}&edition={Edition}&page=3&pageSize=50";

        var state = QuestionListQueryString.Parse(uri);

        state.Search.Should().Be("cráse");
        state.Statuses.Should().Equal(QuestionStatus.Draft, QuestionStatus.Published);
        state.Types.Should().Equal(QuestionType.TrueFalse);
        state.Difficulties.Should().Equal(2, 5);
        state.DifficultyNotSet.Should().BeTrue();
        state.SubjectIds.Should().Equal(Subject);
        state.TopicIds.Should().Equal(TopicA, TopicB);
        state.OrganizerIds.Should().Equal(Organizer);
        state.ExamEditionId.Should().Be(Edition);
        state.Page.Should().Be(2, "the address counts pages from 1");
        state.PageSize.Should().Be(50);
        state.HasFilters.Should().BeTrue();
    }

    [Theory]
    [InlineData("status=Banana&type=1&difficulty=9&difficulty=abc&topic=nao&subject=&organizer=1&edition=zzz&page=0&pageSize=7")]
    [InlineData("status=&type=&difficulty=&search=%20%20")]
    public void Parse_ValuesThatCannotBeRead_AreNoFilter(string query)
    {
        var state = QuestionListQueryString.Parse($"https://localhost/admin/questions?{query}");

        state.Should().Be(new QuestionListState());
    }

    [Fact]
    public void Parse_ARepeatedValue_CountsOnce_AndTheKeysIgnoreCase()
    {
        var state = QuestionListQueryString.Parse(
            $"https://localhost/admin/questions?TOPIC={TopicA}&topic={TopicA}&Status=Published&status=published");

        state.TopicIds.Should().Equal(TopicA);
        state.Statuses.Should().Equal(QuestionStatus.Published);
    }

    [Fact]
    public void Build_AStateAtItsDefaults_IsEmpty()
    {
        QuestionListQueryString.Build(new QuestionListState()).Should().BeEmpty();
    }

    [Fact]
    public void Build_ThenParse_GivesTheSameState()
    {
        var state = new QuestionListState(
            "vírgula & crase",
            [QuestionStatus.Published],
            [QuestionType.SingleChoice, QuestionType.TrueFalse],
            [1, 4],
            DifficultyNotSet: true,
            SubjectIds: [Subject],
            TopicIds: [TopicA, TopicB],
            OrganizerIds: [Organizer],
            ExamEditionId: Edition,
            Page: 2,
            PageSize: 10);

        var built = QuestionListQueryString.Build(state);
        var parsed = QuestionListQueryString.Parse($"https://localhost/admin/questions{built}");

        built.Should().StartWith("?");
        built.Should().Contain("page=3").And.Contain("pageSize=10").And.Contain("difficulty=none")
            .And.Contain($"topic={TopicA}").And.Contain($"topic={TopicB}");
        parsed.Should().BeEquivalentTo(state);
    }

    [Fact]
    public void Carried_KeepsOnlyTheKeysOfTheList()
    {
        var carried = QuestionListQueryString.Carried(
            $"https://localhost/admin/questions/new?status=Draft&topic={TopicA}&returnUrl=%2Fevil&utm=1#top");

        carried.Should().Be($"?status=Draft&topic={TopicA}");
    }

    [Fact]
    public void Carried_AnAddressWithNoKeys_IsEmpty()
    {
        QuestionListQueryString.Carried("https://localhost/admin/questions/new").Should().BeEmpty();
    }

    [Fact]
    public void State_PickedIdCount_CountsSubjectsTopicsAndOrganizersTogether()
    {
        var state = new QuestionListState(SubjectIds: [Subject], TopicIds: [TopicA, TopicB], OrganizerIds: [Organizer]);

        state.PickedIdCount.Should().Be(4);
    }

    [Fact]
    public void State_ToQuery_CarriesTheFiltersAndTheTablesOwnPageAndSearch()
    {
        var state = new QuestionListState(
            Search: "ignored: the table's search is what is sent",
            Statuses: [QuestionStatus.Draft],
            DifficultyNotSet: true,
            TopicIds: [TopicA],
            ExamEditionId: Edition);

        var query = state.ToQuery(page: 4, pageSize: 10, search: "crase");

        query.Page.Should().Be(4);
        query.PageSize.Should().Be(10);
        query.Search.Should().Be("crase");
        query.Statuses.Should().Equal(QuestionStatus.Draft);
        query.DifficultyNotSet.Should().BeTrue();
        query.TopicIds.Should().Equal(TopicA);
        query.ExamEditionId.Should().Be(Edition);
    }
}
