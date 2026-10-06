using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-79 through HTTP: the areas and the subjects back office. The tests share one database, so each works on
/// rows of its own, found by a marker in the name, and never asserts a global count.
/// </summary>
public sealed class SubjectEndpointTests : CatalogApiTests
{
    private const string Areas = "/api/v1/catalog/areas";
    private const string Subjects = "/api/v1/catalog/subjects";
    private const string Topics = "/api/v1/catalog/topics";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<AreaResponse> AreaAsync(HttpClient admin, string code) =>
        (await admin.GetFromJsonAsync<List<AreaResponse>>(Areas, AppJson.Options))!.Single(area => area.Code == code);

    private static async Task<SubjectResponse> CreateAsync(HttpClient admin, string name, Guid? areaId = null)
    {
        var response = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name, areaId), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static async Task<TopicResponse> CreateTopicAsync(HttpClient admin, Guid subjectId, string name)
    {
        var response = await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private static async Task<SubjectPageResponse> ListAsync(HttpClient admin, string query) =>
        (await admin.GetFromJsonAsync<SubjectPageResponse>($"{Subjects}?{query}", AppJson.Options))!;

    // AC2 (API side): the endpoint answers the nine areas in the order of the approved list.
    [Fact]
    public async Task ListAreas_ReturnsTheNineAreasInDisplayOrder()
    {
        var admin = await AdminAsync();

        var areas = await admin.GetFromJsonAsync<List<AreaResponse>>(Areas, AppJson.Options);

        areas!.Select(area => area.Code).Should().Equal(
            "Languages",
            "Mathematics",
            "LogicalReasoning",
            "NaturalSciences",
            "HumanSciences",
            "Law",
            "InformationTechnology",
            "Administration",
            "SpecificKnowledge");
        areas!.Select(area => area.DisplayOrder).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9);
    }

    // AC15: without the permission every route is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();
        var subject = new SaveSubjectRequest("Portugues");
        var topic = new SaveTopicRequest("Crase");

        var responses = new[]
        {
            await student.GetAsync(Areas),
            await student.GetAsync(Subjects),
            await student.GetAsync($"{Subjects}/{id}"),
            await student.PostAsJsonAsync(Subjects, subject, AppJson.Options),
            await student.PutAsJsonAsync($"{Subjects}/{id}", subject, AppJson.Options),
            await student.DeleteAsync($"{Subjects}/{id}"),
            await student.GetAsync($"{Subjects}/{id}/topics"),
            await student.PostAsJsonAsync($"{Subjects}/{id}/topics", topic, AppJson.Options),
            await student.PutAsJsonAsync($"{Topics}/{id}", topic, AppJson.Options),
            await student.DeleteAsync($"{Topics}/{id}"),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    [Fact]
    public async Task List_Anonymous_IsUnauthorized()
    {
        (await Client().GetAsync(Subjects)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC3: a new subject with an area shows in the list with that area and no topics.
    [Fact]
    public async Task Create_WithAnArea_IsListedWithItsAreaAndZeroTopics()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var law = await AreaAsync(admin, "Law");

        var created = await CreateAsync(admin, $"Direito Constitucional {marker}", law.Id);

        created.AreaCode.Should().Be("Law");
        created.TopicCount.Should().Be(0);
        var listed = await ListAsync(admin, $"search={marker}");
        listed.Items.Should().ContainSingle().Which.Should().Be(created);
    }

    // AC4: the name is taken ignoring case and accents, and a deleted subject keeps its name.
    [Fact]
    public async Task Create_NameTakenIgnoringCaseAndAccents_IsRefused_AlsoAfterTheFirstWasDeleted()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var first = await CreateAsync(admin, $"Português {marker}");

        var duplicate = await admin.PostAsJsonAsync(
            Subjects, new SaveSubjectRequest($"PORTUGUES {marker}"), AppJson.Options);
        (await admin.DeleteAsync($"{Subjects}/{first.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterDelete = await admin.PostAsJsonAsync(
            Subjects, new SaveSubjectRequest($"Português {marker}"), AppJson.Options);

        foreach (var response in new[] { duplicate, afterDelete })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameTaken);
        }
    }

    // AC6: shape and area errors answer 400 and write nothing.
    [Fact]
    public async Task Create_InvalidName_IsRefusedWithItsOwnCode_AndNothingIsWritten()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var tooShort = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest("A"), AppJson.Options);
        var tooLong = await admin.PostAsJsonAsync(
            Subjects, new SaveSubjectRequest(marker + new string('x', 150)), AppJson.Options);

        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooShort.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameRequired);
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameTooLong);
        (await ListAsync(admin, $"search={marker}")).Total.Should().Be(0);
    }

    [Fact]
    public async Task Create_UnknownArea_IsRefusedWithAreaInvalid_AndNothingIsWritten()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var response = await admin.PostAsJsonAsync(
            Subjects, new SaveSubjectRequest($"Subject {marker}", Guid.CreateVersion7()), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectAreaInvalid);
        (await ListAsync(admin, $"search={marker}")).Total.Should().Be(0);
    }

    // AC7: the area filter, the "no area" filter and the search.
    [Fact]
    public async Task List_FiltersByAreaWithoutAreaAndSearch()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var law = await AreaAsync(admin, "Law");
        var languages = await AreaAsync(admin, "Languages");
        var constitutional = await CreateAsync(admin, $"Direito Constitucional {marker}", law.Id);
        var portuguese = await CreateAsync(admin, $"Português {marker}", languages.Id);
        var loose = await CreateAsync(admin, $"Sem área {marker}");

        var byLaw = await ListAsync(admin, $"search={marker}&areaId={law.Id}");
        var withoutArea = await ListAsync(admin, $"search={marker}&withoutArea=true");
        var contradictory = await ListAsync(admin, $"search={marker}&areaId={law.Id}&withoutArea=true");
        var bySearch = await ListAsync(admin, $"search=constit {marker}");
        var byPartial = await ListAsync(admin, "search=constit");

        byLaw.Items.Select(item => item.Id).Should().Equal(constitutional.Id);
        withoutArea.Items.Select(item => item.Id).Should().Equal(loose.Id);
        contradictory.Items.Select(item => item.Id).Should().Equal(new[] { loose.Id }, "no area wins over an area");
        bySearch.Items.Should().BeEmpty("the search is one contiguous text, not words");
        byPartial.Items.Should().Contain(item => item.Id == constitutional.Id);
        (await ListAsync(admin, $"search={marker}")).Items.Select(item => item.Id)
            .Should().Equal(constitutional.Id, portuguese.Id, loose.Id);
    }

    [Fact]
    public async Task Update_ChangesNameAndArea_AndAnAreaCanBeCleared()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var law = await AreaAsync(admin, "Law");
        var subject = await CreateAsync(admin, $"Constitucional {marker}", law.Id);

        var renamed = await admin.PutAsJsonAsync(
            $"{Subjects}/{subject.Id}", new SaveSubjectRequest($"Direito Constitucional {marker}"), AppJson.Options);

        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await renamed.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
        body.Name.Should().Be($"Direito Constitucional {marker}");
        body.AreaId.Should().BeNull();
        body.AreaCode.Should().BeNull();
    }

    [Fact]
    public async Task Update_ToTheNameOfAnotherSubject_IsRefused_ButKeepingItsOwnNameIsNot()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var first = await CreateAsync(admin, $"Alpha {marker}");
        var second = await CreateAsync(admin, $"Beta {marker}");

        var clash = await admin.PutAsJsonAsync(
            $"{Subjects}/{second.Id}", new SaveSubjectRequest($"ALPHA {marker}"), AppJson.Options);
        var same = await admin.PutAsJsonAsync(
            $"{Subjects}/{first.Id}", new SaveSubjectRequest($"Alpha {marker}"), AppJson.Options);

        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await clash.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameTaken);
        same.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC8: a subject with a topic cannot be deleted; once the topic goes, it can, and the row stays flagged.
    [Fact]
    public async Task Delete_WithATopic_IsRefused_ThenAllowedOnceTheTopicIsDeleted_AsASoftDelete()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Português {marker}");
        var topic = await CreateTopicAsync(admin, subject.Id, "Crase");

        var blocked = await admin.DeleteAsync($"{Subjects}/{subject.Id}");
        (await admin.DeleteAsync($"{Topics}/{topic.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var allowed = await admin.DeleteAsync($"{Subjects}/{subject.Id}");

        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await blocked.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectHasTopics);
        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(admin, $"search={marker}")).Items.Should().BeEmpty();
        var stored = await QueryAsync(context => context.Subjects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(row => row.Id == subject.Id));
        stored.IsDeleted.Should().BeTrue();
    }

    // AC8: the list carries the topic count, which the screen reads to block the delete.
    [Fact]
    public async Task List_CountsTheTopicsThatWereNotDeleted()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Matemática {marker}");
        await CreateTopicAsync(admin, subject.Id, "Funções");
        var removed = await CreateTopicAsync(admin, subject.Id, "Geometria");
        await admin.DeleteAsync($"{Topics}/{removed.Id}");

        var listed = await ListAsync(admin, $"search={marker}");

        listed.Items.Should().ContainSingle().Which.TopicCount.Should().Be(1);
    }

    // AC13: unknown or deleted ids answer 404.
    [Fact]
    public async Task UnknownOrDeletedId_IsNotFound_OnEverySubjectRoute()
    {
        var admin = await AdminAsync();
        var deleted = await CreateAsync(admin, $"Apagada {Marker()}");
        await admin.DeleteAsync($"{Subjects}/{deleted.Id}");

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var responses = new[]
            {
                await admin.GetAsync($"{Subjects}/{id}"),
                await admin.PutAsJsonAsync($"{Subjects}/{id}", new SaveSubjectRequest("Qualquer"), AppJson.Options),
                await admin.DeleteAsync($"{Subjects}/{id}"),
                await admin.GetAsync($"{Subjects}/{id}/topics"),
                await admin.PostAsJsonAsync($"{Subjects}/{id}/topics", new SaveTopicRequest("Crase"), AppJson.Options),
            };

            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNotFound);
            }
        }
    }
}
