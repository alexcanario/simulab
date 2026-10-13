using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-100 BR10: what the question bank may ask the catalog for the list's filters, through
/// <see cref="ISubjectDirectory"/> and <see cref="IExamEditionDirectory"/>: live rows only, by the catalog's own
/// names, and the difference between "unknown" (absent) and "known and empty" (present with no children).
/// </summary>
public sealed class QuestionFilterDirectoryTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";
    private const string Exams = "/api/v1/catalog/exams";
    private const string Organizers = "/api/v1/catalog/organizers";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private async Task<T> SubjectsAsync<T>(Func<ISubjectDirectory, Task<T>> read)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await read(scope.ServiceProvider.GetRequiredService<ISubjectDirectory>());
    }

    private async Task<T> EditionsAsync<T>(Func<IExamEditionDirectory, Task<T>> read)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await read(scope.ServiceProvider.GetRequiredService<IExamEditionDirectory>());
    }

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

    private static async Task<OrganizerResponse> OrganizerAsync(HttpClient admin, string marker, string prefix = "BN")
    {
        var response = await admin.PostAsJsonAsync(
            Organizers,
            new SaveOrganizerRequest($"Banca {marker}", $"{prefix}{marker[^10..]}".ToUpperInvariant(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
    }

    private static async Task<Guid> ExamAsync(HttpClient admin, string marker)
    {
        var authority = await admin.PostAsJsonAsync(
            "/api/v1/catalog/issuing-authorities", new SaveIssuingAuthorityRequest($"Orgao {marker}"), AppJson.Options);
        authority.StatusCode.Should().Be(HttpStatusCode.Created, await authority.Content.ReadAsStringAsync());
        var authorityId = (await authority.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!.Id;

        var exam = await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authorityId, $"Exame {marker}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options);
        exam.StatusCode.Should().Be(HttpStatusCode.Created, await exam.Content.ReadAsStringAsync());

        return (await exam.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!.Id;
    }

    private static async Task<ExamEditionResponse> EditionAsync(HttpClient admin, Guid examId, Guid organizerId, int year, string status)
    {
        var response = await admin.PostAsJsonAsync(
            $"{Exams}/{examId}/editions", new SaveExamEditionRequest(organizerId, year, null, Status: status), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    [Fact]
    public async Task ListTaxonomy_IsEverySubjectWithItsLiveTopicsByName_AndASubjectWithNoTopicIsEmpty()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await SubjectAsync(admin, $"Zzz Português {marker}");
        var empty = await SubjectAsync(admin, $"Zzz Vazia {marker}");
        await TopicAsync(admin, subject.Id, $"Pontuação {marker}");
        var gone = await TopicAsync(admin, subject.Id, $"Acentuação {marker}");
        await TopicAsync(admin, subject.Id, $"Crase {marker}");
        (await admin.DeleteAsync($"/api/v1/catalog/topics/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var taxonomy = await SubjectsAsync(directory => directory.ListTaxonomyAsync(null, CancellationToken.None));

        var found = taxonomy.Single(entry => entry.Id == subject.Id);
        found.Topics.Select(topic => topic.Name).Should().Equal($"Crase {marker}", $"Pontuação {marker}");
        taxonomy.Single(entry => entry.Id == empty.Id).Topics.Should().BeEmpty();
        taxonomy.Select(entry => entry.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListTaxonomy_WithIds_KeepsOnlyTheLiveSubjectsAmongThem()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var kept = await SubjectAsync(admin, $"Mantida {marker}");
        var deleted = await SubjectAsync(admin, $"Excluída {marker}");
        await TopicAsync(admin, kept.Id, $"Tópico {marker}");
        (await admin.DeleteAsync($"{Subjects}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var found = await SubjectsAsync(directory => directory.ListTaxonomyAsync(
            [kept.Id, deleted.Id, Guid.CreateVersion7()], CancellationToken.None));

        found.Should().ContainSingle().Which.Id.Should().Be(kept.Id);
        found[0].Topics.Should().ContainSingle().Which.Name.Should().Be($"Tópico {marker}");
    }

    [Fact]
    public async Task ListTaxonomy_WithNoIds_IsAnEmptyListAndNotTheWholeTaxonomy()
    {
        await AdminAsync();

        var found = await SubjectsAsync(directory => directory.ListTaxonomyAsync([], CancellationToken.None));

        found.Should().BeEmpty();
    }

    [Fact]
    public async Task ListOrganizers_AreTheLiveOnesByAcronym_AndADeletedOneIsAbsent()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var second = await OrganizerAsync(admin, marker, "ZZB");
        var first = await OrganizerAsync(admin, Marker(), "ZZA");
        var deleted = await OrganizerAsync(admin, Marker(), "ZZC");
        (await admin.DeleteAsync($"{Organizers}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var organizers = await EditionsAsync(directory => directory.ListOrganizersAsync(CancellationToken.None));

        organizers.Select(entry => entry.Id).Should().Contain([first.Id, second.Id]).And.NotContain(deleted.Id);
        organizers.Select(entry => entry.Acronym).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        organizers.Single(entry => entry.Id == second.Id).Should()
            .Be(new OrganizerDirectoryEntry(second.Id, second.Acronym, second.Name));
    }

    [Fact]
    public async Task ListOrganizerEditions_AreTheLiveEditionsOfAnyStatus_ByOrganizer()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var exam = await ExamAsync(admin, marker);
        var organizer = await OrganizerAsync(admin, marker);
        var withNone = await OrganizerAsync(admin, Marker());
        var draft = await EditionAsync(admin, exam, organizer.Id, 2023, nameof(ExamEditionStatus.Draft));
        var published = await EditionAsync(admin, exam, organizer.Id, 2024, nameof(ExamEditionStatus.Published));
        var removed = await EditionAsync(admin, exam, organizer.Id, 2022, nameof(ExamEditionStatus.Draft));
        (await admin.DeleteAsync($"{Exams}/{exam}/editions/{removed.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var unknown = Guid.CreateVersion7();

        var found = await EditionsAsync(directory => directory.ListOrganizerEditionsAsync(
            [organizer.Id, withNone.Id, unknown], CancellationToken.None));

        found.Select(entry => entry.OrganizerId).Should().BeEquivalentTo([organizer.Id, withNone.Id]);
        found.Single(entry => entry.OrganizerId == organizer.Id).ExamEditionIds
            .Should().BeEquivalentTo([draft.Id, published.Id]);
        found.Single(entry => entry.OrganizerId == withNone.Id).ExamEditionIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ListOrganizerEditions_WithNoIds_IsEmpty()
    {
        await AdminAsync();

        var found = await EditionsAsync(directory => directory.ListOrganizerEditionsAsync([], CancellationToken.None));

        found.Should().BeEmpty();
    }
}
