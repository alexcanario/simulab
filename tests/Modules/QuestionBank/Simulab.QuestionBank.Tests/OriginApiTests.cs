using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// What the origin tests of F-96 share: an exam, an organizer, an edition and its notice subjects, made
/// through the catalog routes the way an admin would, and the four read routes of the question bank.
/// </summary>
public abstract class OriginApiTests : QuestionBankApiTests
{
    protected const string QuestionBank = "/api/v1/question-bank";
    protected const string CatalogExams = "/api/v1/catalog/exams";

    /// <summary>A catalog edition with the names a source line is built from.</summary>
    protected sealed record Edition(
        Guid Id,
        Guid ExamId,
        string ExamName,
        string OrganizerAcronym,
        string OrganizerName,
        int Year,
        string? Position);

    protected static async Task<Edition> EditionAsync(
        HttpClient admin,
        int year = 2024,
        string? position = null,
        string status = "Draft",
        string? marker = null)
    {
        marker ??= Marker();
        var authority = await admin.PostAsJsonAsync(
            "/api/v1/catalog/issuing-authorities",
            new SaveIssuingAuthorityRequest($"Orgao {marker}"),
            AppJson.Options);
        authority.StatusCode.Should().Be(HttpStatusCode.Created, await authority.Content.ReadAsStringAsync());
        var authorityId = (await authority.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!.Id;

        var examName = $"Exame {marker}";
        var exam = await admin.PostAsJsonAsync(
            CatalogExams,
            new SaveExamRequest(authorityId, examName, nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options);
        exam.StatusCode.Should().Be(HttpStatusCode.Created, await exam.Content.ReadAsStringAsync());
        var examId = (await exam.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!.Id;

        var acronym = $"BN{marker[^10..]}".ToUpperInvariant();
        var organizerName = $"Banca {marker}";
        var organizer = await admin.PostAsJsonAsync(
            "/api/v1/catalog/organizers",
            new SaveOrganizerRequest(organizerName, acronym, nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        organizer.StatusCode.Should().Be(HttpStatusCode.Created, await organizer.Content.ReadAsStringAsync());
        var organizerId = (await organizer.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!.Id;

        var edition = await admin.PostAsJsonAsync(
            $"{CatalogExams}/{examId}/editions",
            new SaveExamEditionRequest(organizerId, year, position, Status: status),
            AppJson.Options);
        edition.StatusCode.Should().Be(HttpStatusCode.Created, await edition.Content.ReadAsStringAsync());
        var editionId = (await edition.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!.Id;

        return new Edition(editionId, examId, examName, acronym, organizerName, year, position);
    }

    protected static async Task<NoticeSubjectResponse> NoticeSubjectAsync(
        HttpClient admin,
        Edition edition,
        string? label = null,
        string? group = "Conhecimentos básicos")
    {
        var response = await admin.PostAsJsonAsync(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}/notice-subjects",
            new SaveNoticeSubjectRequest(group, label ?? $"Português {Marker()}", 10),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<NoticeSubjectResponse>(AppJson.Options))!;
    }

    /// <summary>The edition is published the way an admin does it: the same form, with the status changed.</summary>
    protected static async Task PublishEditionAsync(HttpClient admin, Edition edition)
    {
        var current = (await admin.GetFromJsonAsync<ExamEditionResponse>(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}", AppJson.Options))!;
        var response = await admin.PutAsJsonAsync(
            $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}",
            new SaveExamEditionRequest(
                current.OrganizerId,
                current.NoticeYear,
                current.Position,
                current.NoticeReference,
                current.NoticeUrl,
                current.AppliedOn,
                nameof(ExamEditionStatus.Published)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    protected static SaveQuestionRequest FromEdition(
        Guid topicId,
        Guid? editionId,
        int? number,
        Guid? noticeSubjectId = null,
        string statement = "<p>Qual é a capital da França?</p>") =>
        Complete(topicId, statement) with
        {
            ExamEditionId = editionId,
            PaperNumber = number,
            NoticeSubjectId = noticeSubjectId
        };

    protected static Task<HttpResponseMessage> UpdateAsync(HttpClient client, Guid id, SaveQuestionRequest request) =>
        client.PutAsJsonAsync($"{Questions}/{id}", request, AppJson.Options);

    protected static async Task<QuestionResponse> UpdatedAsync(HttpClient client, Guid id, SaveQuestionRequest request)
    {
        var response = await UpdateAsync(client, id, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    protected static async Task<EditionQuestionPageResponse> EditionQuestionsAsync(
        HttpClient client,
        Guid editionId,
        string query = "") =>
        (await client.GetFromJsonAsync<EditionQuestionPageResponse>(
            $"{QuestionBank}/editions/{editionId}/questions{query}", AppJson.Options))!;
}
