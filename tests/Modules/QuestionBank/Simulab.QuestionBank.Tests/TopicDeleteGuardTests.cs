using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-95 BR13 and AC9 through HTTP: a topic is refused its delete while a live question of any status
/// points at it, and the catalog's own list says so. The guard is the catalog asking every registered
/// <see cref="ITopicUsageCheck"/>; the catalog never references this module.
/// </summary>
public sealed class TopicDeleteGuardTests : QuestionBankApiTests
{
    private const string Catalog = "/api/v1/catalog";

    private static Task<HttpResponseMessage> DeleteTopicAsync(HttpClient admin, Guid topicId) =>
        admin.DeleteAsync($"{Catalog}/topics/{topicId}");

    [Fact]
    public async Task DeleteTopic_WithALiveDraftQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Complete(topic.Id));

        var response = await DeleteTopicAsync(admin, topic.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task DeleteTopic_WithAPublishedQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await PublishedAsync(admin, topic.Id);

        var response = await DeleteTopicAsync(admin, topic.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task TopicList_FlagsATopicAQuestionUses_AndFreesItWhenTheQuestionIsDeleted()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await admin.PostAsJsonAsync(
            $"{Catalog}/subjects", new SaveSubjectRequest($"Matemática {marker}"), AppJson.Options);
        var subjectId = (await subject.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;
        var used = (await (await admin.PostAsJsonAsync(
            $"{Catalog}/subjects/{subjectId}/topics", new SaveTopicRequest("Usado"), AppJson.Options))
            .Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
        var free = (await (await admin.PostAsJsonAsync(
            $"{Catalog}/subjects/{subjectId}/topics", new SaveTopicRequest("Livre"), AppJson.Options))
            .Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
        var question = await CreatedAsync(admin, Complete(used.Id));

        var before = (await admin.GetFromJsonAsync<List<TopicResponse>>(
            $"{Catalog}/subjects/{subjectId}/topics", AppJson.Options))!;
        await admin.DeleteAsync($"{Questions}/{question.Id}");
        var after = (await admin.GetFromJsonAsync<List<TopicResponse>>(
            $"{Catalog}/subjects/{subjectId}/topics", AppJson.Options))!;

        before.Single(topic => topic.Id == used.Id).InUse.Should().BeTrue();
        before.Single(topic => topic.Id == free.Id).InUse.Should().BeFalse();
        after.Single(topic => topic.Id == used.Id).InUse.Should().BeFalse();
    }

    // AC9: once the question is deleted the topic can go; a deleted question does not guard it.
    [Fact]
    public async Task DeleteTopic_AfterTheQuestionIsDeleted_Succeeds()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id));
        (await DeleteTopicAsync(admin, topic.Id)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await admin.DeleteAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var response = await DeleteTopicAsync(admin, topic.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    // BR13: moving the topic to another subject is allowed while it is in use, and the question follows it.
    [Fact]
    public async Task MoveTopic_ToAnotherSubject_IsAllowedWhileAQuestionUsesIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id));
        var target = await admin.PostAsJsonAsync(
            $"{Catalog}/subjects", new SaveSubjectRequest($"Destino {Marker()}"), AppJson.Options);
        var targetId = (await target.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;

        var response = await admin.PutAsJsonAsync(
            $"{Catalog}/topics/{topic.Id}", new SaveTopicRequest(topic.Name, targetId.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await GetAsync(admin, question.Id)).SubjectId.Should().Be(targetId.Id);
    }
}
