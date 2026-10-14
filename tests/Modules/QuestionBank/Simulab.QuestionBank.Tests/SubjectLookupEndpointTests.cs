using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// B-28 BR1 to BR4: the subject and topic lookups of the question form belong to the question bank and need
/// <c>questions.manage</c> only, so a curator with no <c>catalog.manage</c> can pick a topic. Subjects are made
/// through the catalog routes the way an admin would; each test works on rows of its own, found by a marker.
/// </summary>
public sealed class SubjectLookupEndpointTests : QuestionBankApiTests
{
    private const string QuestionBank = "/api/v1/question-bank";
    private const string CatalogSubjects = "/api/v1/catalog/subjects";

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(CatalogSubjects, new SaveSubjectRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static async Task<TopicResponse> TopicOfAsync(HttpClient admin, Guid subjectId, string name)
    {
        var response = await admin.PostAsJsonAsync(
            $"{CatalogSubjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private static async Task<List<SubjectLookupResponse>> SearchAsync(HttpClient client, string search) =>
        (await client.GetFromJsonAsync<List<SubjectLookupResponse>>(
            $"{QuestionBank}/subjects?search={Uri.EscapeDataString(search)}", AppJson.Options))!;

    // AC1: the seeded Curator holds questions.manage and no catalog.manage; before the fix this route did not exist.
    [Fact]
    public async Task Search_ByACuratorWithNoCatalogPermission_AnswersTheMatchingSubjectsOnly_IgnoringCaseAndAccents()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var marker = Marker();
        var math = await SubjectAsync(admin, $"Matemática {marker}");
        await SubjectAsync(admin, $"Português {marker}");

        var found = await SearchAsync(curator, $"MATEMATICA {marker}");

        found.Should().ContainSingle().Which.Should().Be(new SubjectLookupResponse(math.Id, $"Matemática {marker}"));
    }

    // AC1
    [Fact]
    public async Task Search_ListsSubjectsByName_AtMostTwenty()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var marker = Marker();
        foreach (var index in Enumerable.Range(0, 21).Reverse())
        {
            await SubjectAsync(admin, $"Disciplina {marker} {index:00}");
        }

        var found = await SearchAsync(curator, $"disciplina {marker}");

        found.Count.Should().Be(20);
        found.Select(subject => subject.Name).Should().BeInAscendingOrder()
            .And.Equal(Enumerable.Range(0, 20).Select(index => $"Disciplina {marker} {index:00}"));
    }

    // AC1: a subject with no topic is listed, as the catalog route does today (BR1).
    [Fact]
    public async Task Search_ListsASubjectWithNoTopic()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var subject = await SubjectAsync(admin, $"Sem tópicos {Marker()}");

        var found = await SearchAsync(curator, subject.Name);

        found.Should().ContainSingle().Which.Id.Should().Be(subject.Id);
    }

    // AC2
    [Fact]
    public async Task Topics_ByACurator_AreTheLiveOnesByName_WithoutTheDeletedOne()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");
        await TopicOfAsync(admin, subject.Id, "Pontuação");
        var gone = await TopicOfAsync(admin, subject.Id, "Acentuação");
        await TopicOfAsync(admin, subject.Id, "Crase");
        (await admin.DeleteAsync($"/api/v1/catalog/topics/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await curator.GetAsync($"{QuestionBank}/subjects/{subject.Id}/topics");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var topics = await response.Content.ReadFromJsonAsync<List<TopicLookupResponse>>(AppJson.Options);
        topics!.Select(topic => topic.Name).Should().Equal("Crase", "Pontuação");
    }

    // AC3
    [Fact]
    public async Task Topics_OfAnUnknownSubject_Are404SubjectNotFound()
    {
        var curator = await CuratorAsync();

        var response = await curator.GetAsync($"{QuestionBank}/subjects/{Guid.CreateVersion7()}/topics");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionSubjectNotFound);
    }

    // AC3
    [Fact]
    public async Task Topics_OfADeletedSubject_Are404SubjectNotFound()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var subject = await SubjectAsync(admin, $"Apagada {Marker()}");
        (await admin.DeleteAsync($"{CatalogSubjects}/{subject.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await curator.GetAsync($"{QuestionBank}/subjects/{subject.Id}/topics");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionSubjectNotFound);
    }

    // AC4: no questions.manage, no lookup.
    [Fact]
    public async Task EveryRoute_ForAStudent_Answers403()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var subject = await SubjectAsync(admin, $"Direito {Marker()}");

        var responses = new[]
        {
            await student.GetAsync($"{QuestionBank}/subjects"),
            await student.GetAsync($"{QuestionBank}/subjects/{subject.Id}/topics")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    // AC4: catalog.manage alone does not open the question bank's lookups.
    [Fact]
    public async Task EveryRoute_ForAUserWithOnlyCatalogManage_Answers403()
    {
        var admin = await AdminAsync();
        var role = await admin.PostAsJsonAsync(
            "/api/v1/identity/roles",
            new SaveRoleRequest($"Taxonomy only {Marker()}", [CatalogPermissions.Manage]),
            AppJson.Options);
        role.StatusCode.Should().Be(HttpStatusCode.Created, await role.Content.ReadAsStringAsync());
        var roleName = (await role.Content.ReadFromJsonAsync<RoleResponse>(AppJson.Options))!.Name;
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: roleName);
        var taxonomist = await TestAccounts.SignedInAsync(Client(), account.Email!);
        var subject = await SubjectAsync(admin, $"Direito {Marker()}");

        var catalog = await taxonomist.GetAsync(CatalogSubjects);
        var responses = new[]
        {
            await taxonomist.GetAsync($"{QuestionBank}/subjects"),
            await taxonomist.GetAsync($"{QuestionBank}/subjects/{subject.Id}/topics")
        };

        catalog.StatusCode.Should().Be(HttpStatusCode.OK, "the role really holds catalog.manage");
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
