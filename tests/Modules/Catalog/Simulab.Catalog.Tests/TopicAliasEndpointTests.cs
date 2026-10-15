using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-77 through HTTP, topic side: a topic's other names are unique inside its subject, follow the topic when it
/// moves, and go with it when it is deleted. Each test works on rows of its own, found by a marker in the text.
/// </summary>
public sealed class TopicAliasEndpointTests : CatalogApiTests
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

    private static async Task<HttpResponseMessage> PostTopicAsync(
        HttpClient admin,
        Guid subjectId,
        string name,
        params string[] aliases) =>
        await admin.PostAsJsonAsync(
            $"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name, null, aliases), AppJson.Options);

    private static async Task<TopicResponse> TopicAsync(
        HttpClient admin,
        Guid subjectId,
        string name,
        params string[] aliases)
    {
        var response = await PostTopicAsync(admin, subjectId, name, aliases);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private static async Task<HttpResponseMessage> PutTopicAsync(
        HttpClient admin,
        Guid topicId,
        string name,
        Guid? subjectId,
        IReadOnlyList<string>? aliases) =>
        await admin.PutAsJsonAsync($"{Topics}/{topicId}", new SaveTopicRequest(name, subjectId, aliases), AppJson.Options);

    private static async Task<List<TopicResponse>> ListTopicsAsync(HttpClient admin, Guid subjectId) =>
        (await admin.GetFromJsonAsync<List<TopicResponse>>($"{Subjects}/{subjectId}/topics", AppJson.Options))!;

    private Task<List<bool>> StoredDeletedFlagsAsync(Guid topicId) =>
        QueryAsync(context => context.Database
            .SqlQuery<bool>($"""
                SELECT is_deleted AS "Value" FROM catalog.topic_aliases
                WHERE topic_id = {topicId} ORDER BY normalized_name
                """)
            .ToListAsync());

    // AC2 (topic side): the create answer and the list of the subject's topics carry the aliases, ordered.
    [Fact]
    public async Task Create_WithAliases_AnswersAndListsThemOrderedAndWithoutRepeats()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Português {marker}");

        var created = await TopicAsync(
            admin, subject.Id, "Interpretação de texto", $"Leitura {marker}", $"Compreensão {marker}", $"COMPREENSAO {marker}");

        var expected = new[] { $"Compreensão {marker}", $"Leitura {marker}" };
        created.Aliases.Should().Equal(expected);
        (await ListTopicsAsync(admin, subject.Id)).Should().ContainSingle().Which.Aliases.Should().Equal(expected);
    }

    // AC3: 1 and 201 characters are refused with their own code and nothing is written.
    [Theory]
    [InlineData(1, CatalogErrorCodes.TopicAliasTooShort)]
    [InlineData(201, CatalogErrorCodes.TopicAliasTooLong)]
    public async Task Create_AliasOutOfRange_IsRefused_AndNothingIsWritten(int length, string code)
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");

        var response = await PostTopicAsync(admin, subject.Id, "Crase", new string('x', length));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        (await ListTopicsAsync(admin, subject.Id)).Should().BeEmpty();
    }

    // AC4: equal to its own name once normalized.
    [Fact]
    public async Task Create_AliasEqualToTheName_IsRefused()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");

        var response = await PostTopicAsync(admin, subject.Id, "Interpretação de texto", "interpretacao DE texto");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicAliasSameAsName);
        (await ListTopicsAsync(admin, subject.Id)).Should().BeEmpty();
    }

    // AC6: the same alias under two subjects is allowed; inside one subject it clashes with another topic's alias
    // and a topic cannot be renamed to it.
    [Fact]
    public async Task Alias_IsUniqueInsideItsSubject_AndFreeAcrossSubjects()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var portuguese = await SubjectAsync(admin, $"Português {marker}");
        var english = await SubjectAsync(admin, $"Inglês {marker}");
        var first = await TopicAsync(admin, portuguese.Id, $"Interpretação {marker}", $"Interpretação {marker}x");
        var second = await TopicAsync(admin, english.Id, $"Reading {marker}", $"Interpretação {marker}x");
        var sibling = await TopicAsync(admin, portuguese.Id, $"Crase {marker}");

        var asAlias = await PutTopicAsync(admin, sibling.Id, sibling.Name, null, [$"interpretacao {marker}x"]);
        var asName = await PutTopicAsync(admin, sibling.Id, $"interpretacao {marker}x", null, null);

        first.Aliases.Should().Equal($"Interpretação {marker}x");
        second.Aliases.Should().Equal($"Interpretação {marker}x");
        asAlias.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await asAlias.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicAliasTaken);
        asName.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await asName.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicNameTaken);
    }

    // AC6 (BR7): an alias equal to a sibling topic's name is the alias conflict.
    [Fact]
    public async Task Alias_EqualToASiblingTopicsName_IsRefused()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Português {marker}");
        await TopicAsync(admin, subject.Id, $"Crase {marker}");
        var other = await TopicAsync(admin, subject.Id, $"Acentuação {marker}");

        var response = await PutTopicAsync(admin, other.Id, other.Name, null, [$"CRASE {marker}"]);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicAliasTaken);
    }

    // AC9: a move that clashes in the target subject is refused and nothing moves; a clean move takes the aliases.
    [Fact]
    public async Task Move_ClashingInTheTargetSubject_IsRefused_ThenAClearMoveTakesTheAliasesAlong()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var origin = await SubjectAsync(admin, $"Origem {marker}");
        var crowded = await SubjectAsync(admin, $"Cheio {marker}");
        var clear = await SubjectAsync(admin, $"Livre {marker}");
        var moving = await TopicAsync(admin, origin.Id, $"Gramática {marker}", $"Crase {marker}");
        await TopicAsync(admin, crowded.Id, $"Crase {marker}");

        var refused = await PutTopicAsync(admin, moving.Id, moving.Name, crowded.Id, null);
        var stayed = await ListTopicsAsync(admin, origin.Id);
        var moved = await PutTopicAsync(admin, moving.Id, moving.Name, clear.Id, null);

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicAliasTaken);
        stayed.Should().ContainSingle().Which.Aliases.Should().Equal($"Crase {marker}");
        moved.StatusCode.Should().Be(HttpStatusCode.OK, await moved.Content.ReadAsStringAsync());
        (await ListTopicsAsync(admin, origin.Id)).Should().BeEmpty();
        (await ListTopicsAsync(admin, clear.Id)).Should().ContainSingle()
            .Which.Aliases.Should().Equal($"Crase {marker}");
    }

    // AC10: deleting a topic removes its aliases with it, and a removed alias frees its text for a sibling.
    [Fact]
    public async Task Delete_TopicWithAliases_RemovesItsAliasesWithIt()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Português {marker}");
        var topic = await TopicAsync(admin, subject.Id, $"Interpretação {marker}", $"Leitura {marker}", $"Compreensão {marker}");

        var response = await admin.DeleteAsync($"{Topics}/{topic.Id}");
        var reuse = await PostTopicAsync(admin, subject.Id, $"Outro {marker}", $"Leitura {marker}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredDeletedFlagsAsync(topic.Id)).Should().Equal(true, true);
        reuse.StatusCode.Should().Be(HttpStatusCode.Created, await reuse.Content.ReadAsStringAsync());
    }

    // AC13: a null list on an update leaves the set unchanged, and the answer carries it.
    [Fact]
    public async Task Update_WithAliasesNull_LeavesTheSetUnchanged()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Português {marker}");
        var topic = await TopicAsync(admin, subject.Id, $"Interpretação {marker}", $"Leitura {marker}");

        var response = await PutTopicAsync(admin, topic.Id, $"Interpretação de texto {marker}", null, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!.Aliases
            .Should().Equal($"Leitura {marker}");
        (await ListTopicsAsync(admin, subject.Id)).Single().Aliases.Should().Equal($"Leitura {marker}");
    }

    // AC15 (API side): without the permission a student cannot write aliases.
    [Fact]
    public async Task Save_WithAliases_AsAStudent_IsForbidden()
    {
        var student = await StudentAsync();
        var request = new SaveTopicRequest("Crase", null, ["Acento grave"]);
        var id = Guid.CreateVersion7();

        var create = await student.PostAsJsonAsync($"{Subjects}/{id}/topics", request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Topics}/{id}", request, AppJson.Options);

        foreach (var response in new[] { create, update })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }
}
