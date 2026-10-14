using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>F-76 BR10 and AC9: the catalog list stays light; the syllabus travels only with the exam's own page.</summary>
public sealed class PublishedExamListSyllabusTests : CatalogApiTests
{
    private const string Catalog = "/api/v1/catalog";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    [Fact]
    public async Task List_CarriesNoNoticeSubjects()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Marker();
        var authority = (await (await admin.PostAsJsonAsync(
            Catalog + "/issuing-authorities", new SaveIssuingAuthorityRequest($"Orgao {token}", Marker()), AppJson.Options))
            .Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!;
        var exam = (await (await admin.PostAsJsonAsync(
            Catalog + "/exams",
            new SaveExamRequest(authority.Id, $"Exame {token}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options)).Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
        var board = (await (await admin.PostAsJsonAsync(
            Catalog + "/organizers", new SaveOrganizerRequest($"Banca {token}", Marker(), nameof(OrganizerKind.ExamBoard)), AppJson.Options))
            .Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
        var edition = (await (await admin.PostAsJsonAsync(
            $"{Catalog}/exams/{exam.Id}/editions",
            new SaveExamEditionRequest(board.Id, 2025, null, null, null, null, "Published"),
            AppJson.Options)).Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
        (await admin.PostAsJsonAsync(
            $"{Catalog}/exams/{exam.Id}/editions/{edition.Id}/notice-subjects",
            new SaveNoticeSubjectRequest(null, $"Disciplina {token}", 10, null),
            AppJson.Options)).IsSuccessStatusCode.Should().BeTrue();

        var response = await student.GetAsync($"{Catalog}/published-exams?search={Uri.EscapeDataString(token)}");
        var text = await response.Content.ReadAsStringAsync();

        text.Should().Contain($"Exame {token}");
        text.Should().NotContainEquivalentOf("noticeSubjects").And.NotContain($"Disciplina {token}");
    }
}
