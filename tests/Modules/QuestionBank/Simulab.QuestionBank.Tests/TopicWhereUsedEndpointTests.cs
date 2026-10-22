using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-90 BR3 through HTTP: a topic's "where used" counts the live questions of any status that point at it, through
/// the question bank's <see cref="ITopicUsageCheck"/>, beside the notice subjects that map it. A deleted question is
/// not counted, and the count is above 0 exactly when the catalog's list says the topic is in use.
/// </summary>
public sealed class TopicWhereUsedEndpointTests : QuestionReviewApiTests
{
    private const string Catalog = "/api/v1/catalog";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    private static async Task<WhereUsedResponse> WhereUsedAsync(HttpClient client, Guid topicId) =>
        await ReadAsync<WhereUsedResponse>(await client.GetAsync($"{Catalog}/topics/{topicId}/where-used"));

    /// <summary>A live notice subject of a new edition that maps the topic.</summary>
    private static async Task<(ExamResponse Exam, ExamEditionResponse Edition, NoticeSubjectResponse Row)> MappedAsync(
        HttpClient admin,
        Guid topicId)
    {
        var authority = await ReadAsync<IssuingAuthorityResponse>(await admin.PostAsJsonAsync(
            $"{Catalog}/issuing-authorities", new SaveIssuingAuthorityRequest($"Orgao {Marker()}", Marker()), AppJson.Options));
        var exam = await ReadAsync<ExamResponse>(await admin.PostAsJsonAsync(
            $"{Catalog}/exams",
            new SaveExamRequest(authority.Id, $"Guarda {Marker()}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options));
        var board = await ReadAsync<OrganizerResponse>(await admin.PostAsJsonAsync(
            $"{Catalog}/organizers", new SaveOrganizerRequest($"Banca {Marker()}", Marker(), nameof(OrganizerKind.ExamBoard)), AppJson.Options));
        var edition = await ReadAsync<ExamEditionResponse>(await admin.PostAsJsonAsync(
            $"{Catalog}/exams/{exam.Id}/editions",
            new SaveExamEditionRequest(board.Id, TimeProvider.System.GetUtcNow().Year),
            AppJson.Options));
        var row = await ReadAsync<NoticeSubjectResponse>(await admin.PostAsJsonAsync(
            $"{Catalog}/exams/{exam.Id}/editions/{edition.Id}/notice-subjects",
            new SaveNoticeSubjectRequest(null, "Lingua Portuguesa", null, [new NoticeSubjectMappingRequest(TopicId: topicId)]),
            AppJson.Options));

        return (exam, edition, row);
    }

    // AC4, UC2, BR3, BR4: one mapping, and three live questions (draft, in review, published) counted; a deleted one is not.
    [Fact]
    public async Task Topic_MappedAndUsedByQuestionsOfEveryStatus_ShowsTheMappingAndCountsTheLiveQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var (exam, edition, row) = await MappedAsync(admin, topic.Id);
        await CreatedAsync(admin, Complete(topic.Id));
        await InReviewAsync(admin, topic.Id);
        await PublishedAsync(admin, topic.Id);
        var deleted = await CreatedAsync(admin, Complete(topic.Id));
        (await admin.DeleteAsync($"{Questions}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var whereUsed = await WhereUsedAsync(admin, topic.Id);

        whereUsed.Mappings.Should().Equal(
            new WhereUsedMappingResponse(exam.Id, exam.Name, edition.Id, edition.NoticeYear, null, row.Id, "Lingua Portuguesa"));
        whereUsed.QuestionCount.Should().Be(3);
    }

    // AC6, BR3: a topic used only by questions has no mapping row and the question count.
    [Fact]
    public async Task Topic_UsedOnlyByQuestions_HasNoMappingAndTheCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Complete(topic.Id));
        await CreatedAsync(admin, Complete(topic.Id));

        var whereUsed = await WhereUsedAsync(admin, topic.Id);

        whereUsed.Mappings.Should().BeEmpty();
        whereUsed.QuestionCount.Should().Be(2);
    }

    // BR8, BR9: the count and the list's InUse read the same rows, so an in-use row never opens on the empty state.
    [Fact]
    public async Task Count_IsAboveZeroExactlyWhileTheListSaysTheTopicIsInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id));
        var subjectId = topic.SubjectId;

        var usedCount = (await WhereUsedAsync(admin, topic.Id)).QuestionCount;
        var usedFlag = (await admin.GetFromJsonAsync<List<TopicResponse>>($"{Catalog}/subjects/{subjectId}/topics", AppJson.Options))!
            .Single().InUse;
        (await admin.DeleteAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var freeCount = (await WhereUsedAsync(admin, topic.Id)).QuestionCount;
        var freeFlag = (await admin.GetFromJsonAsync<List<TopicResponse>>($"{Catalog}/subjects/{subjectId}/topics", AppJson.Options))!
            .Single().InUse;

        (usedCount, usedFlag).Should().Be((1, true));
        (freeCount, freeFlag).Should().Be((0, false));
    }
}
