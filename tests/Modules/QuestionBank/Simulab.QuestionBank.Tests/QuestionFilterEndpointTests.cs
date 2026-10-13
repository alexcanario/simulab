using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-100 through HTTP, on a real PostgreSQL: the filters of the question list. Every test marks its own
/// questions with a word of the statement and searches for it, because the tests of one class share a database.
/// </summary>
public sealed class QuestionFilterEndpointTests : OriginApiTests
{
    private static async Task<QuestionResponse> TrueFalseAsync(HttpClient admin, Guid topicId, string statement) =>
        await CreatedAsync(
            admin,
            new SaveQuestionRequest(topicId, "pt-BR", statement, Type: QuestionType.TrueFalse, TrueFalseKey: true));

    private static async Task<TopicResponse> AnotherTopicAsync(HttpClient admin, Guid subjectId, string name)
    {
        var response = await admin.PostAsJsonAsync(
            $"/api/v1/catalog/subjects/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private static async Task<Guid> OrganizerOfAsync(HttpClient admin, Edition edition) =>
        (await admin.GetFromJsonAsync<ExamEditionResponse>(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}", AppJson.Options))!.OrganizerId;

    private static IEnumerable<Guid> Ids(QuestionPageResponse page) => page.Items.Select(item => item.Id);

    // AC1, BR1: a topic filter keeps the questions of the picked topics; with no topic picked every topic is listed.
    [Fact]
    public async Task List_ByTopics_KeepsOnlyTheQuestionsOfThePickedOnes()
    {
        var admin = await AdminAsync();
        var first = await TopicAsync(admin);
        var second = await TopicAsync(admin);
        var third = await TopicAsync(admin);
        var marker = Marker();
        var inFirst = await CreatedAsync(admin, Complete(first.Id, $"<p>Um {marker}</p>"));
        var inSecond = await CreatedAsync(admin, Complete(second.Id, $"<p>Dois {marker}</p>"));
        var inThird = await CreatedAsync(admin, Complete(third.Id, $"<p>Tres {marker}</p>"));

        var picked = await ListAsync(admin, $"?search={marker}&topic={first.Id}&topic={second.Id}");
        var all = await ListAsync(admin, $"?search={marker}");

        Ids(picked).Should().BeEquivalentTo([inFirst.Id, inSecond.Id]);
        picked.Total.Should().Be(2);
        Ids(all).Should().BeEquivalentTo([inFirst.Id, inSecond.Id, inThird.Id]);
    }

    // AC2, BR2: a subject means every topic it has when the list is read, one added later included; a subject and
    // a topic of another subject are a union.
    [Fact]
    public async Task List_BySubject_MeansEveryTopicOfIt_AndIsAUnionWithPickedTopics()
    {
        var admin = await AdminAsync();
        var firstTopic = await TopicAsync(admin);
        var elsewhere = await TopicAsync(admin);
        var marker = Marker();
        var inFirst = await CreatedAsync(admin, Complete(firstTopic.Id, $"<p>Um {marker}</p>"));
        var inElsewhere = await CreatedAsync(admin, Complete(elsewhere.Id, $"<p>Outra {marker}</p>"));

        var added = await AnotherTopicAsync(admin, firstTopic.SubjectId, $"Acrescentado {marker}");
        var inAdded = await CreatedAsync(admin, Complete(added.Id, $"<p>Novo {marker}</p>"));

        var bySubject = await ListAsync(admin, $"?search={marker}&subject={firstTopic.SubjectId}");
        var union = await ListAsync(admin, $"?search={marker}&subject={firstTopic.SubjectId}&topic={elsewhere.Id}");

        Ids(bySubject).Should().BeEquivalentTo([inFirst.Id, inAdded.Id]);
        Ids(union).Should().BeEquivalentTo([inFirst.Id, inAdded.Id, inElsewhere.Id]);
    }

    // AC3, BR3: an organizer keeps what came from its editions; an authored question has no edition and is left out.
    [Fact]
    public async Task List_ByOrganizer_KeepsTheQuestionsOfItsEditions_AndLeavesOutAuthoredOnes()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var edition = await EditionAsync(admin);
        var other = await EditionAsync(admin);
        var organizerId = await OrganizerOfAsync(admin, edition);
        var fromEdition = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, statement: $"<p>Prova {marker}</p>"));
        await CreatedAsync(admin, FromEdition(topic.Id, other.Id, 1, statement: $"<p>Outra prova {marker}</p>"));
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Autoral {marker}</p>"));

        var page = await ListAsync(admin, $"?search={marker}&organizer={organizerId}");

        Ids(page).Should().Equal(fromEdition.Id);
        page.Total.Should().Be(1);
    }

    // AC4, BR3: an edition and an organizer that does not own it give nothing.
    [Fact]
    public async Task List_ByAnEditionAndAForeignOrganizer_IsEmpty()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var edition = await EditionAsync(admin);
        var other = await EditionAsync(admin);
        var foreignOrganizer = await OrganizerOfAsync(admin, other);
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, statement: $"<p>Prova {marker}</p>"));

        var foreign = await ListAsync(admin, $"?search={marker}&edition={edition.Id}&organizer={foreignOrganizer}");
        var own = await ListAsync(admin, $"?search={marker}&edition={edition.Id}");

        foreign.Items.Should().BeEmpty();
        foreign.Total.Should().Be(0);
        own.Items.Should().ContainSingle();
    }

    // AC5, BR1, BR4: type and status take several values; between them it is AND.
    [Fact]
    public async Task List_ByTypeAndStatus_KeepsWhatMatchesEveryFilter()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var singleDraft = await CreatedAsync(admin, Complete(topic.Id, $"<p>Escolha rascunho {marker}</p>"));
        var singlePublished = await PublishedWithAsync(admin, Complete(topic.Id, $"<p>Escolha publicada {marker}</p>"));
        var trueFalseDraft = await TrueFalseAsync(admin, topic.Id, $"<p>Certo rascunho {marker}</p>");
        var trueFalsePublished = await TrueFalseAsync(admin, topic.Id, $"<p>Certo publicado {marker}</p>");
        (await admin.PostAsync($"{Questions}/{trueFalsePublished.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var onlyPublishedTrueFalse = await ListAsync(admin, $"?search={marker}&type=TrueFalse&status=Published");
        var bothStatuses = await ListAsync(admin, $"?search={marker}&status=Draft&status=Published&type=SingleChoice");
        var bothTypes = await ListAsync(admin, $"?search={marker}&type=SingleChoice&type=TrueFalse&status=Draft");

        Ids(onlyPublishedTrueFalse).Should().Equal(trueFalsePublished.Id);
        Ids(bothStatuses).Should().BeEquivalentTo([singleDraft.Id, singlePublished.Id]);
        Ids(bothTypes).Should().BeEquivalentTo([singleDraft.Id, trueFalseDraft.Id]);
    }

    private static async Task<QuestionResponse> PublishedWithAsync(HttpClient admin, SaveQuestionRequest request)
    {
        var question = await CreatedAsync(admin, request);
        var response = await admin.PostAsync($"{Questions}/{question.Id}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return question;
    }

    // AC6, BR4: "none" is a question with no difficulty; it joins the numbers with OR.
    [Fact]
    public async Task List_ByDifficultyNoneAndFive_KeepsBothOfThem()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var unset = await CreatedAsync(admin, Complete(topic.Id, $"<p>Sem {marker}</p>"));
        var five = await CreatedAsync(admin, Complete(topic.Id, $"<p>Cinco {marker}</p>", difficulty: 5));
        var two = await CreatedAsync(admin, Complete(topic.Id, $"<p>Dois {marker}</p>", difficulty: 2));

        var both = await ListAsync(admin, $"?search={marker}&difficulty=none&difficulty=5");
        var onlyNumbers = await ListAsync(admin, $"?search={marker}&difficulty=2&difficulty=5");
        var onlyNone = await ListAsync(admin, $"?search={marker}&difficulty=none");

        Ids(both).Should().BeEquivalentTo([unset.Id, five.Id]);
        Ids(onlyNumbers).Should().BeEquivalentTo([two.Id, five.Id]);
        Ids(onlyNone).Should().Equal(unset.Id);
    }

    // AC7, BR1, BR5, BR7: filters and a statement word together, newest first and paged.
    [Fact]
    public async Task List_FiltersAndSearchTogether_AreNewestFirstAndPaged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var elsewhere = await TopicAsync(admin);
        var marker = Marker();
        var first = await CreatedAsync(admin, Complete(topic.Id, $"<p>Primeira {marker}</p>", difficulty: 3));
        var second = await CreatedAsync(admin, Complete(topic.Id, $"<p>Segunda {marker}</p>", difficulty: 3));
        var third = await CreatedAsync(admin, Complete(topic.Id, $"<p>Terceira {marker}</p>", difficulty: 3));
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Dificil {marker}</p>", difficulty: 5));
        await CreatedAsync(admin, Complete(elsewhere.Id, $"<p>Fora {marker}</p>", difficulty: 3));

        var filter = $"?search={marker}&topic={topic.Id}&difficulty=3";
        var page0 = await ListAsync(admin, $"{filter}&page=0&pageSize=2");
        var page1 = await ListAsync(admin, $"{filter}&page=1&pageSize=2");

        page0.Total.Should().Be(3);
        Ids(page0).Should().Equal(third.Id, second.Id);
        Ids(page1).Should().Equal(first.Id);
    }

    // AC8, BR6: a value that is not understood is ignored, not refused.
    [Fact]
    public async Task List_ValuesThatAreNotUnderstood_AreIgnored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Escolha {marker}</p>"));
        var trueFalse = await TrueFalseAsync(admin, topic.Id, $"<p>Certo {marker}</p>");

        // The one value that is understood (type=TrueFalse) still applies; the others change nothing.
        var response = await admin.GetAsync(
            $"{Questions}?search={marker}&type=TrueFalse&type=Banana&status=Banana&topic=nao-e-guid&subject=&organizer=1&edition=zzz&difficulty=9&difficulty=abc");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var page = (await response.Content.ReadFromJsonAsync<QuestionPageResponse>(AppJson.Options))!;
        Ids(page).Should().Equal(trueFalse.Id);
    }

    // AC10, BR6: a well-formed id that no topic, subject, organizer or edition has is dropped; the filter does not
    // restrict, and an edition that is gone leaves the organizers alone to decide.
    [Fact]
    public async Task List_IdsThatNothingHas_AreDropped()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Escolha {marker}</p>"));
        var trueFalse = await TrueFalseAsync(admin, topic.Id, $"<p>Certo {marker}</p>");

        // Not "nothing matches an unknown topic": the unknown ids are dropped and the type still restricts.
        var page = await ListAsync(
            admin,
            $"?search={marker}&type=TrueFalse&topic={Guid.CreateVersion7()}&subject={Guid.CreateVersion7()}&organizer={Guid.CreateVersion7()}&edition={Guid.CreateVersion7()}");

        Ids(page).Should().Equal(trueFalse.Id);
    }

    // AC15, BR6: the largest address the page can build is answered.
    [Fact]
    public async Task List_HundredTopicIds_AreReadWhole()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var other = await TopicAsync(admin);
        var marker = Marker();
        var inTopic = await CreatedAsync(admin, Complete(topic.Id, $"<p>Um {marker}</p>"));
        await CreatedAsync(admin, Complete(other.Id, $"<p>Dois {marker}</p>"));
        var strangers = Enumerable.Range(0, QuestionListQuery.MaxFilterValues - 1).Select(_ => $"topic={Guid.CreateVersion7()}");
        var query = string.Join('&', strangers.Append($"topic={topic.Id}"));

        // The real topic is the hundredth value: the whole list is read, so only its question is kept.
        var response = await admin.GetAsync($"{Questions}?search={marker}&{query}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var page = (await response.Content.ReadFromJsonAsync<QuestionPageResponse>(AppJson.Options))!;
        Ids(page).Should().Equal(inTopic.Id);
    }

    // BR6: more than the cap is read only up to the cap; the rest is ignored.
    [Fact]
    public async Task List_MoreIdsThanTheCap_ReadsOnlyTheFirstOnes()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var other = await TopicAsync(admin);
        var marker = Marker();
        var inTopic = await CreatedAsync(admin, Complete(topic.Id, $"<p>Um {marker}</p>"));
        var inOther = await CreatedAsync(admin, Complete(other.Id, $"<p>Dois {marker}</p>"));
        await TrueFalseAsync(admin, other.Id, $"<p>Certo {marker}</p>");
        var strangers = Enumerable.Range(0, QuestionListQuery.MaxFilterValues).Select(_ => $"topic={Guid.CreateVersion7()}");
        var query = string.Join('&', strangers.Append($"topic={topic.Id}"));

        // The real topic is the 101st value, so it is ignored: only the hundred strangers apply, nothing
        // resolves, the topic filter is dropped and both single-choice questions are listed (the type filter
        // still keeps the true/false one out). Read whole, it would list one.
        var page = await ListAsync(admin, $"?search={marker}&type=SingleChoice&{query}");

        Ids(page).Should().BeEquivalentTo([inTopic.Id, inOther.Id]);
    }

    // BR14 of F-95 carried over: the new routes need the permission too.
    [Fact]
    public async Task Lookups_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();

        (await student.GetAsync($"{QuestionBank}/taxonomy")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await student.GetAsync($"{QuestionBank}/organizers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Taxonomy_ListsEverySubjectWithItsTopicsByName_ForACuratorWithNoCatalogPermission()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var marker = Marker();
        var subject = await admin.PostAsJsonAsync(
            "/api/v1/catalog/subjects", new SaveSubjectRequest($"Zzz Disciplina {marker}"), AppJson.Options);
        var subjectId = (await subject.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;
        await AnotherTopicAsync(admin, subjectId, $"Beta {marker}");
        await AnotherTopicAsync(admin, subjectId, $"Alfa {marker}");
        var empty = await admin.PostAsJsonAsync(
            "/api/v1/catalog/subjects", new SaveSubjectRequest($"Zzz Vazia {marker}"), AppJson.Options);
        var emptyId = (await empty.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;

        var response = await curator.GetAsync($"{QuestionBank}/taxonomy");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var taxonomy = (await response.Content.ReadFromJsonAsync<IReadOnlyList<QuestionTaxonomySubjectResponse>>(AppJson.Options))!;
        var found = taxonomy.Single(item => item.Id == subjectId);
        found.Name.Should().Be($"Zzz Disciplina {marker}");
        found.Topics.Select(topic => topic.Name).Should().Equal($"Alfa {marker}", $"Beta {marker}");
        taxonomy.Single(item => item.Id == emptyId).Topics.Should().BeEmpty();
        taxonomy.Select(item => item.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Organizers_ListsEveryLiveOrganizerByAcronym_ForACuratorWithNoCatalogPermission()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var edition = await EditionAsync(admin);
        var organizerId = await OrganizerOfAsync(admin, edition);

        var response = await curator.GetAsync($"{QuestionBank}/organizers");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var organizers = (await response.Content.ReadFromJsonAsync<IReadOnlyList<OrganizerLookupResponse>>(AppJson.Options))!;
        var found = organizers.Single(item => item.Id == organizerId);
        found.Acronym.Should().Be(edition.OrganizerAcronym);
        found.Name.Should().Be(edition.OrganizerName);
        organizers.Select(item => item.Acronym).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }
}
