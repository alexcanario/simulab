using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-79 through HTTP: the topics under a subject. The tests share one database, so each creates its own
/// subjects, named with a marker.
/// </summary>
public sealed class TopicEndpointTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";
    private const string Topics = "/api/v1/catalog/topics";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static Task<HttpResponseMessage> AddAsync(HttpClient admin, Guid subjectId, string name) =>
        admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);

    private static async Task<TopicResponse> CreateAsync(HttpClient admin, Guid subjectId, string name)
    {
        var response = await AddAsync(admin, subjectId, name);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private static async Task<IReadOnlyList<TopicResponse>> ListAsync(HttpClient admin, Guid subjectId) =>
        (await admin.GetFromJsonAsync<List<TopicResponse>>($"{Subjects}/{subjectId}/topics", AppJson.Options))!;

    // AC9: the topics of a subject come back alphabetically, accents ignored.
    [Fact]
    public async Task List_ReturnsTheTopicsAlphabetically()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Direito Constitucional {Marker()}");

        await CreateAsync(admin, subject.Id, "Controle de constitucionalidade");
        await CreateAsync(admin, subject.Id, "Aplicabilidade das normas");
        await CreateAsync(admin, subject.Id, "Émile");

        (await ListAsync(admin, subject.Id)).Select(topic => topic.Name).Should().Equal(
            "Aplicabilidade das normas",
            "Controle de constitucionalidade",
            "Émile");
    }

    // AC10: a name is unique inside its subject, ignoring case; the same name in another subject is allowed.
    [Fact]
    public async Task Create_NameTakenInTheSameSubject_IsRefused_ButFreeInAnotherSubject()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var portuguese = await SubjectAsync(admin, $"Português {marker}");
        var literature = await SubjectAsync(admin, $"Literatura {marker}");
        await CreateAsync(admin, portuguese.Id, "Crase");

        var duplicate = await AddAsync(admin, portuguese.Id, "crase");
        var elsewhere = await AddAsync(admin, literature.Id, "crase");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await duplicate.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameTaken);
        elsewhere.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_InvalidName_IsRefusedWithItsOwnCode()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");

        var tooShort = await AddAsync(admin, subject.Id, "A");
        var tooLong = await AddAsync(admin, subject.Id, new string('x', CatalogLimits.TopicNameMaxLength + 1));

        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooShort.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameRequired);
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameTooLong);
        (await ListAsync(admin, subject.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_IgnoresTheSubjectInTheBody_TheRouteWins()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var routed = await SubjectAsync(admin, $"Routed {marker}");
        var other = await SubjectAsync(admin, $"Other {marker}");

        var response = await admin.PostAsJsonAsync(
            $"{Subjects}/{routed.Id}/topics", new SaveTopicRequest("Crase", other.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListAsync(admin, routed.Id)).Should().ContainSingle();
        (await ListAsync(admin, other.Id)).Should().BeEmpty();
    }

    // AC11: an update may move the topic; a name already in the target subject refuses the move.
    [Fact]
    public async Task Update_MovesTheTopicToAnotherSubject()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var from = await SubjectAsync(admin, $"From {marker}");
        var to = await SubjectAsync(admin, $"To {marker}");
        var topic = await CreateAsync(admin, from.Id, "Crase");

        var response = await admin.PutAsJsonAsync(
            $"{Topics}/{topic.Id}", new SaveTopicRequest("Crase", to.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync(admin, from.Id)).Should().BeEmpty();
        (await ListAsync(admin, to.Id)).Should().ContainSingle().Which.Id.Should().Be(topic.Id);
    }

    [Fact]
    public async Task Update_MoveToASubjectThatAlreadyHasTheName_IsRefused_AndTheTopicStays()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var from = await SubjectAsync(admin, $"From {marker}");
        var to = await SubjectAsync(admin, $"To {marker}");
        var topic = await CreateAsync(admin, from.Id, "Crase");
        await CreateAsync(admin, to.Id, "CRASE");

        var response = await admin.PutAsJsonAsync(
            $"{Topics}/{topic.Id}", new SaveTopicRequest("Crase", to.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameTaken);
        (await ListAsync(admin, from.Id)).Should().ContainSingle().Which.Id.Should().Be(topic.Id);
    }

    // BR10: the target subject must exist and not be deleted.
    [Fact]
    public async Task Update_MoveToAnUnknownOrDeletedSubject_IsNotFound()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var from = await SubjectAsync(admin, $"From {marker}");
        var deleted = await SubjectAsync(admin, $"Deleted {marker}");
        await admin.DeleteAsync($"{Subjects}/{deleted.Id}");
        var topic = await CreateAsync(admin, from.Id, "Crase");

        foreach (var target in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await admin.PutAsJsonAsync(
                $"{Topics}/{topic.Id}", new SaveTopicRequest("Crase", target), AppJson.Options);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNotFound);
        }

        (await ListAsync(admin, from.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task Update_WithoutASubject_RenamesInPlace_AndKeepingItsOwnNameIsNotATaken()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");
        var topic = await CreateAsync(admin, subject.Id, "Crase");

        var same = await admin.PutAsJsonAsync($"{Topics}/{topic.Id}", new SaveTopicRequest("CRASE"), AppJson.Options);
        var renamed = await admin.PutAsJsonAsync($"{Topics}/{topic.Id}", new SaveTopicRequest("Acentuação"), AppJson.Options);

        same.StatusCode.Should().Be(HttpStatusCode.OK);
        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await renamed.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!.SubjectId.Should().Be(subject.Id);
    }

    // AC12: deleting a topic takes it out of the section and lowers the subject's count.
    [Fact]
    public async Task Delete_RemovesTheTopicFromTheListAndLowersTheCount()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Português {marker}");
        var topic = await CreateAsync(admin, subject.Id, "Crase");
        await CreateAsync(admin, subject.Id, "Pontuação");

        var response = await admin.DeleteAsync($"{Topics}/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(admin, subject.Id)).Select(item => item.Name).Should().Equal("Pontuação");
        (await admin.GetFromJsonAsync<SubjectResponse>($"{Subjects}/{subject.Id}", AppJson.Options))!
            .TopicCount.Should().Be(1);
    }

    // BR7: a deleted topic keeps its name taken inside its subject.
    [Fact]
    public async Task Create_NameOfADeletedTopic_IsStillTaken()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");
        var topic = await CreateAsync(admin, subject.Id, "Crase");
        await admin.DeleteAsync($"{Topics}/{topic.Id}");

        var response = await AddAsync(admin, subject.Id, "Crase");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameTaken);
    }

    // AC13: unknown or deleted ids answer 404.
    [Fact]
    public async Task UnknownOrDeletedTopic_IsNotFound_OnUpdateAndDelete()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");
        var deleted = await CreateAsync(admin, subject.Id, "Crase");
        await admin.DeleteAsync($"{Topics}/{deleted.Id}");

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var responses = new[]
            {
                await admin.PutAsJsonAsync($"{Topics}/{id}", new SaveTopicRequest("Qualquer"), AppJson.Options),
                await admin.DeleteAsync($"{Topics}/{id}"),
            };

            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNotFound);
            }
        }
    }
}
