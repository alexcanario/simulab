using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-101 AC10, BR8 (review finding 4): an annulled question still points at its edition and its notice subject,
/// so the catalog refuses their delete; the usage checks filter on no status and this keeps it so.
/// </summary>
public sealed class AnnulledQuestionUsageGuardTests : OriginApiTests
{
    private static string EditionRoute(Edition edition) => $"{CatalogExams}/{edition.ExamId}/editions/{edition.Id}";

    private static async Task<QuestionResponse> AnnulledFromAsync(HttpClient admin, SaveQuestionRequest request)
    {
        var question = await CreatedAsync(admin, request);
        (await admin.PostAsync($"{Questions}/{question.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var annulled = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        annulled.StatusCode.Should().Be(HttpStatusCode.OK, await annulled.Content.ReadAsStringAsync());

        return question;
    }

    [Fact]
    public async Task DeleteEdition_WithAnAnnulledQuestion_IsRefusedWithHasQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await AnnulledFromAsync(admin, FromEdition(topic.Id, edition.Id, 1));

        var response = await admin.DeleteAsync(EditionRoute(edition));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionHasQuestions);
    }

    [Fact]
    public async Task DeleteNoticeSubject_WithAnAnnulledQuestion_IsRefusedWithHasQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var subject = await NoticeSubjectAsync(admin, edition);
        await AnnulledFromAsync(admin, FromEdition(topic.Id, edition.Id, 1, subject.Id));

        var response = await admin.DeleteAsync($"{EditionRoute(edition)}/notice-subjects/{subject.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectHasQuestions);
    }
}
