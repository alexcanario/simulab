using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-76 through HTTP, as the student page calls it: the notice subjects each published edition carries on the exam's
/// detail call. The tests share one database, so each one works on its own exam, board, subjects and topics.
/// </summary>
public sealed class PublishedExamSyllabusEndpointTests : CatalogApiTests
{
    private const string Catalog = "/api/v1/catalog";
    private const string Exams = Catalog + "/exams";
    private const string Subjects = Catalog + "/subjects";
    private const string Topics = Catalog + "/topics";
    private const string Published = Catalog + "/published-exams";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static string SubjectsOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-subjects";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    private static async Task<(Guid Exam, Guid Board)> NewExamAsync(HttpClient admin)
    {
        var authority = await ReadAsync<IssuingAuthorityResponse>(await admin.PostAsJsonAsync(
            Catalog + "/issuing-authorities",
            new SaveIssuingAuthorityRequest($"Orgao {Marker()}", Marker()),
            AppJson.Options));
        var exam = await ReadAsync<ExamResponse>(await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authority.Id, $"Exame {Marker()}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options));
        var board = await ReadAsync<OrganizerResponse>(await admin.PostAsJsonAsync(
            Catalog + "/organizers",
            new SaveOrganizerRequest($"Banca {Marker()}", Marker(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options));

        return (exam.Id, board.Id);
    }

    private static async Task<Guid> EditionAsync(HttpClient admin, Guid exam, Guid board, int year, string status) =>
        (await ReadAsync<ExamEditionResponse>(await admin.PostAsJsonAsync(
            $"{Exams}/{exam}/editions",
            new SaveExamEditionRequest(board, year, null, null, null, null, status),
            AppJson.Options))).Id;

    private static async Task<NoticeSubjectResponse> RowAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        string? group = null,
        int? questions = null,
        params NoticeSubjectMappingRequest[] mappings) =>
        await ReadAsync<NoticeSubjectResponse>(await admin.PostAsJsonAsync(
            SubjectsOf(exam, edition),
            new SaveNoticeSubjectRequest(group, label, questions, mappings),
            AppJson.Options));

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name) =>
        await ReadAsync<SubjectResponse>(await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"{name} {Marker()}"), AppJson.Options));

    private static async Task<TopicResponse> TopicAsync(HttpClient admin, Guid subjectId, string name) =>
        await ReadAsync<TopicResponse>(await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options));

    private static async Task<PublishedExamDetailResponse> FindAsync(HttpClient client, Guid exam) =>
        (await client.GetFromJsonAsync<PublishedExamDetailResponse>($"{Published}/{exam}", AppJson.Options))!;

    // AC1 and AC2: display order, group, number and the mapping of each row, whole subjects and topics.
    [Fact]
    public async Task Find_PublishedEdition_CarriesItsNoticeSubjectsInDisplayOrderWithTheirMappings()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var (exam, board) = await NewExamAsync(admin);
        var edition = await EditionAsync(admin, exam, board, 2025, "Published");
        var math = await SubjectAsync(admin, "Matematica");
        var logic = await SubjectAsync(admin, "Raciocinio Logico");
        var propositions = await TopicAsync(admin, logic.Id, "Proposicoes");
        await RowAsync(admin, exam, edition, "Portugues", "Gerais", 10);
        await RowAsync(admin, exam, edition, "Matematica", "Gerais", null, new NoticeSubjectMappingRequest(SubjectId: math.Id), new NoticeSubjectMappingRequest(TopicId: propositions.Id));
        await RowAsync(admin, exam, edition, "Legislacao", "Especificos", 5);

        var detail = await FindAsync(student, exam);

        var rows = detail.Editions.Should().ContainSingle().Subject.NoticeSubjects!;
        rows.Select(row => row.Label).Should().Equal("Portugues", "Matematica", "Legislacao");
        rows.Select(row => row.Group).Should().Equal("Gerais", "Gerais", "Especificos");
        rows.Select(row => row.QuestionCount).Should().Equal(10, null, 5);
        rows[0].Mappings.Should().BeNullOrEmpty();
        rows[1].Mappings.Should().BeEquivalentTo(
        [
            new NoticeSubjectMappingResponse(math.Id, math.Name, null, null),
            new NoticeSubjectMappingResponse(logic.Id, logic.Name, propositions.Id, "Proposicoes")
        ]);
    }

    // AC4: an edition with none carries an empty list, never a missing one.
    [Fact]
    public async Task Find_PublishedEditionWithoutNoticeSubjects_CarriesAnEmptyList()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var (exam, board) = await NewExamAsync(admin);
        await EditionAsync(admin, exam, board, 2025, "Published");

        var detail = await FindAsync(student, exam);

        detail.Editions.Should().ContainSingle().Which.NoticeSubjects.Should().NotBeNull().And.BeEmpty();
    }

    // AC7: a draft edition never reaches the student side, nor do its notice subjects.
    [Fact]
    public async Task Find_DraftEdition_NeverCarriesItsNoticeSubjects()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var (exam, board) = await NewExamAsync(admin);
        var published = await EditionAsync(admin, exam, board, 2024, "Published");
        var draft = await EditionAsync(admin, exam, board, 2025, "Draft");
        var marker = Marker();
        await RowAsync(admin, exam, published, $"Visivel {marker}");
        await RowAsync(admin, exam, draft, $"Rascunho {marker}");

        var response = await student.GetAsync($"{Published}/{exam}");
        var text = await response.Content.ReadAsStringAsync();
        var detail = await FindAsync(student, exam);

        text.Should().NotContain($"Rascunho {marker}");
        var only = detail.Editions.Should().ContainSingle().Subject;
        only.Id.Should().Be(published);
        only.NoticeSubjects!.Select(row => row.Label).Should().Equal($"Visivel {marker}");
    }

    // AC7: a deleted notice subject is not returned.
    [Fact]
    public async Task Find_DeletedNoticeSubject_IsNotReturned()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var (exam, board) = await NewExamAsync(admin);
        var edition = await EditionAsync(admin, exam, board, 2025, "Published");
        await RowAsync(admin, exam, edition, "Fica");
        var gone = await RowAsync(admin, exam, edition, "Some");
        (await admin.DeleteAsync($"{SubjectsOf(exam, edition)}/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await FindAsync(student, exam);

        detail.Editions.Single().NoticeSubjects!.Select(row => row.Label).Should().Equal("Fica");
    }

    // AC8 (F-75 BR8): a topic that moved to another subject is shown under its new subject.
    [Fact]
    public async Task Find_TopicMovedToAnotherSubject_ShowsTheNewSubject()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var (exam, board) = await NewExamAsync(admin);
        var edition = await EditionAsync(admin, exam, board, 2025, "Published");
        var from = await SubjectAsync(admin, "Direito");
        var to = await SubjectAsync(admin, "Logica");
        var topic = await TopicAsync(admin, from.Id, "Proposicoes");
        await RowAsync(admin, exam, edition, "Raciocinio", null, null, new NoticeSubjectMappingRequest(TopicId: topic.Id));
        (await admin.PutAsJsonAsync($"{Topics}/{topic.Id}", new SaveTopicRequest("Proposicoes", to.Id), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await FindAsync(student, exam);

        var entry = detail.Editions.Single().NoticeSubjects!.Single().Mappings!.Should().ContainSingle().Subject;
        entry.SubjectId.Should().Be(to.Id);
        entry.SubjectName.Should().Be(to.Name);
        entry.TopicName.Should().Be("Proposicoes");
    }
}
