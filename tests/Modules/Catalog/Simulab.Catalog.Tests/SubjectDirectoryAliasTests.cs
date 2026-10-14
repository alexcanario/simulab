using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-77 AC12 (UC4, BR11): what another module asks through <see cref="ISubjectDirectory.FindByNameAsync"/>:
/// every live subject and topic whose name or other name has the normalized form of the text, each topic with
/// its subject, and never a deleted subject, topic or alias.
/// </summary>
public sealed class SubjectDirectoryAliasTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";
    private const string Topics = "/api/v1/catalog/topics";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name, params string[] aliases)
    {
        var response = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name, null, aliases), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static async Task<TopicResponse> TopicAsync(
        HttpClient admin,
        Guid subjectId,
        string name,
        params string[] aliases)
    {
        var response = await admin.PostAsJsonAsync(
            $"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name, null, aliases), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private async Task<TaxonomyNameMatch> FindAsync(string? name)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<ISubjectDirectory>()
            .FindByNameAsync(name, CancellationToken.None);
    }

    [Fact]
    public async Task FindByName_ReturnsSubjectsAndTopicsByNameOrAlias_EachTopicWithItsSubject()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var portuguese = await SubjectAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");
        var english = await SubjectAsync(admin, $"Inglês {marker}");
        var reading = await TopicAsync(admin, portuguese.Id, $"Interpretação de texto {marker}", $"Compreensão {marker}");
        var reading2 = await TopicAsync(admin, english.Id, $"Interpretação de texto {marker}");

        var bySubjectAlias = await FindAsync($"portugues {marker}");
        var byTopicAlias = await FindAsync($"COMPREENSAO {marker}");
        var byTopicName = await FindAsync($"interpretacao de texto {marker}");
        var bySubjectName = await FindAsync($"lingua portuguesa {marker}");
        var unknown = await FindAsync($"Inexistente {marker}");

        bySubjectAlias.Subjects.Should().ContainSingle().Which
            .Should().Be(new SubjectDirectoryEntry(portuguese.Id, $"Língua Portuguesa {marker}"));
        bySubjectAlias.Topics.Should().BeEmpty();
        byTopicAlias.Subjects.Should().BeEmpty();
        byTopicAlias.Topics.Should().ContainSingle().Which.Should().Be(new TopicDirectoryEntry(
            reading.Id, $"Interpretação de texto {marker}", portuguese.Id, $"Língua Portuguesa {marker}"));
        byTopicName.Subjects.Should().BeEmpty();
        byTopicName.Topics.Select(topic => topic.Id).Should().Equal(reading2.Id, reading.Id);
        byTopicName.Topics.Select(topic => topic.SubjectName).Should().Equal(
            $"Inglês {marker}", $"Língua Portuguesa {marker}");
        bySubjectName.Subjects.Should().ContainSingle().Which.Id.Should().Be(portuguese.Id);
        unknown.Subjects.Should().BeEmpty();
        unknown.Topics.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindByName_EmptyText_MatchesNothing(string? name)
    {
        var match = await FindAsync(name);

        match.Subjects.Should().BeEmpty();
        match.Topics.Should().BeEmpty();
    }

    [Fact]
    public async Task FindByName_NeverReturnsADeletedSubjectTopicOrAlias()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Matemática {marker}", $"Aritmética {marker}");
        var topic = await TopicAsync(admin, subject.Id, $"Frações {marker}", $"Divisão {marker}");
        var gone = await SubjectAsync(admin, $"Geografia {marker}", $"Mapas {marker}");
        var goneTopic = await TopicAsync(admin, gone.Id, $"Relevo {marker}", $"Serras {marker}");

        // the alias of a subject is removed, the topic is deleted, then the whole subject
        (await admin.PutAsJsonAsync(
            $"{Subjects}/{subject.Id}", new SaveSubjectRequest(subject.Name, null, []), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.DeleteAsync($"{Topics}/{topic.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Topics}/{goneTopic.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Subjects}/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var text in new[] { $"aritmetica {marker}", $"divisao {marker}", $"frações {marker}", $"geografia {marker}", $"mapas {marker}", $"serras {marker}" })
        {
            var match = await FindAsync(text);
            match.Subjects.Should().BeEmpty(text);
            match.Topics.Should().BeEmpty(text);
        }

        (await FindAsync($"matematica {marker}")).Subjects.Should().ContainSingle()
            .Which.Id.Should().Be(subject.Id);
    }
}
