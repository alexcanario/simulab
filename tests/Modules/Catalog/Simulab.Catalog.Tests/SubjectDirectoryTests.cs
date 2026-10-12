using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// B-28 BR3: what the question bank may ask about subjects through <see cref="ISubjectDirectory"/>: live rows
/// only, the catalog's own normalized name, and the topics of a subject that is gone are absent, not empty.
/// </summary>
public sealed class SubjectDirectoryTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static async Task<TopicResponse> TopicAsync(HttpClient admin, Guid subjectId, string name)
    {
        var response = await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!;
    }

    private async Task<T> DirectoryAsync<T>(Func<ISubjectDirectory, Task<T>> read)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await read(scope.ServiceProvider.GetRequiredService<ISubjectDirectory>());
    }

    [Fact]
    public async Task Search_FindsLiveSubjectsByTheNormalizedName_AndLeavesOutADeletedOne()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var kept = await SubjectAsync(admin, $"Matemática {marker}");
        var deleted = await SubjectAsync(admin, $"Matemática financeira {marker}");
        (await admin.DeleteAsync($"{Subjects}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var found = await DirectoryAsync(directory => directory.SearchAsync($"MATEMATICA {marker}", 20, CancellationToken.None));

        found.Should().ContainSingle().Which.Should().Be(new SubjectDirectoryEntry(kept.Id, $"Matemática {marker}"));
    }

    [Fact]
    public async Task Search_OrdersByNameAndKeepsToTheTake()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        await SubjectAsync(admin, $"Zoologia {marker}");
        await SubjectAsync(admin, $"Álgebra {marker}");
        await SubjectAsync(admin, $"Biologia {marker}");

        var found = await DirectoryAsync(directory => directory.SearchAsync(marker, 2, CancellationToken.None));

        found.Select(entry => entry.Name).Should().Equal($"Álgebra {marker}", $"Biologia {marker}");
    }

    [Fact]
    public async Task ListTopics_AreTheLiveOnesByName_AndASubjectWithNoTopicIsEmptyNotNull()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, $"Português {Marker()}");
        var empty = await SubjectAsync(admin, $"Vazia {Marker()}");
        await TopicAsync(admin, subject.Id, "Pontuação");
        var gone = await TopicAsync(admin, subject.Id, "Acentuação");
        await TopicAsync(admin, subject.Id, "Crase");
        (await admin.DeleteAsync($"/api/v1/catalog/topics/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var topics = await DirectoryAsync(directory => directory.ListTopicsAsync(subject.Id, CancellationToken.None));
        var none = await DirectoryAsync(directory => directory.ListTopicsAsync(empty.Id, CancellationToken.None));

        topics.Should().NotBeNull();
        topics!.Select(topic => topic.Name).Should().Equal("Crase", "Pontuação");
        topics.Should().OnlyContain(topic => topic.SubjectId == subject.Id && topic.SubjectName == subject.Name);
        none.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ListTopics_OfAnUnknownOrDeletedSubject_IsNull()
    {
        var admin = await AdminAsync();
        var deleted = await SubjectAsync(admin, $"Apagada {Marker()}");
        (await admin.DeleteAsync($"{Subjects}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ofDeleted = await DirectoryAsync(directory => directory.ListTopicsAsync(deleted.Id, CancellationToken.None));
        var ofUnknown = await DirectoryAsync(directory => directory.ListTopicsAsync(Guid.CreateVersion7(), CancellationToken.None));

        ofDeleted.Should().BeNull();
        ofUnknown.Should().BeNull();
    }
}
