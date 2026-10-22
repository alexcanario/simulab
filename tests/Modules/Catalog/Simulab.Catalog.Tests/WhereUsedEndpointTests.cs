using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-90 through HTTP, as the Web calls it: what maps a subject whole or a topic, in the BR5 order, live rows only
/// (BR4), with the 404 and 403 of BR10 and BR11. The questions a topic counts are the question bank's and are
/// tested there. The tests share one database, so each works on its own exams, editions, subjects and topics.
/// </summary>
public sealed class WhereUsedEndpointTests : CatalogApiTests
{
    private const string Catalog = "/api/v1/catalog";
    private const string Exams = Catalog + "/exams";
    private const string Subjects = Catalog + "/subjects";
    private const string Topics = Catalog + "/topics";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static int ThisYear => TimeProvider.System.GetUtcNow().Year;

    private static string SubjectsOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-subjects";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    private static async Task<ExamResponse> ExamAsync(HttpClient admin, string name)
    {
        var authority = await ReadAsync<IssuingAuthorityResponse>(await admin.PostAsJsonAsync(
            Catalog + "/issuing-authorities",
            new SaveIssuingAuthorityRequest($"Orgao {Marker()}", Marker()),
            AppJson.Options));

        return await ReadAsync<ExamResponse>(await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authority.Id, name, nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options));
    }

    private static async Task<ExamEditionResponse> EditionAsync(HttpClient admin, Guid examId, int year, string? position = null)
    {
        var board = await ReadAsync<OrganizerResponse>(await admin.PostAsJsonAsync(
            Catalog + "/organizers",
            new SaveOrganizerRequest($"Banca {Marker()}", Marker(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options));

        return await ReadAsync<ExamEditionResponse>(await admin.PostAsJsonAsync(
            $"{Exams}/{examId}/editions",
            new SaveExamEditionRequest(board.Id, year, position),
            AppJson.Options));
    }

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name) =>
        await ReadAsync<SubjectResponse>(await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"{name} {Marker()}"), AppJson.Options));

    private static async Task<TopicResponse> TopicAsync(HttpClient admin, Guid subjectId, string name) =>
        await ReadAsync<TopicResponse>(await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options));

    private static async Task<NoticeSubjectResponse> NoticeSubjectAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        NoticeSubjectMappingRequest mapping) =>
        await ReadAsync<NoticeSubjectResponse>(await admin.PostAsJsonAsync(
            SubjectsOf(exam, edition),
            new SaveNoticeSubjectRequest(null, label, null, [mapping]),
            AppJson.Options));

    private static NoticeSubjectMappingRequest Whole(Guid id) => new(SubjectId: id);

    private static NoticeSubjectMappingRequest Topic(Guid id) => new(TopicId: id);

    private static async Task<WhereUsedResponse> WhereUsedAsync(HttpClient client, string path) =>
        await ReadAsync<WhereUsedResponse>(await client.GetAsync(path));

    private static async Task<string> CodeOfAsync(HttpResponseMessage response) =>
        CodeOf(await response.Content.ReadAsStringAsync());

    // AC1, UC1, BR2, BR5: every live notice subject that maps the subject whole, by exam name (accents ignored),
    // notice year descending, position, then the notice subject's own order; each row carries the ids of its edition page.
    [Fact]
    public async Task Subject_MappedWholeByNoticeSubjectsOfSeveralEditions_ListsThemInTheBr5Order()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var zeta = await ExamAsync(admin, $"Zeta {marker}");
        var alfa = await ExamAsync(admin, $"Álfa {marker}");
        var alfaOld = await EditionAsync(admin, alfa.Id, ThisYear - 2);
        var alfaNew = await EditionAsync(admin, alfa.Id, ThisYear, "Agente");
        var zetaNew = await EditionAsync(admin, zeta.Id, ThisYear);
        var math = await SubjectAsync(admin, "Matematica");

        var zetaRow = await NoticeSubjectAsync(admin, zeta.Id, zetaNew.Id, "Matematica Zeta", Whole(math.Id));
        var oldRow = await NoticeSubjectAsync(admin, alfa.Id, alfaOld.Id, "Matematica antiga", Whole(math.Id));
        var firstRow = await NoticeSubjectAsync(admin, alfa.Id, alfaNew.Id, "Matematica basica", Whole(math.Id));
        var secondRow = await NoticeSubjectAsync(admin, alfa.Id, alfaNew.Id, "Matematica financeira", Whole(math.Id));

        var whereUsed = await WhereUsedAsync(admin, $"{Subjects}/{math.Id}/where-used");

        whereUsed.QuestionCount.Should().Be(0);
        whereUsed.Mappings.Should().Equal(
            new WhereUsedMappingResponse(alfa.Id, alfa.Name, alfaNew.Id, ThisYear, "Agente", firstRow.Id, "Matematica basica"),
            new WhereUsedMappingResponse(alfa.Id, alfa.Name, alfaNew.Id, ThisYear, "Agente", secondRow.Id, "Matematica financeira"),
            new WhereUsedMappingResponse(alfa.Id, alfa.Name, alfaOld.Id, ThisYear - 2, null, oldRow.Id, "Matematica antiga"),
            new WhereUsedMappingResponse(zeta.Id, zeta.Name, zetaNew.Id, ThisYear, null, zetaRow.Id, "Matematica Zeta"));
    }

    // AC2, BR4: a mapping of a deleted notice subject or of a deleted edition is not listed, and the subject is not in use.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Subject_WhoseOnlyMappingIsDeleted_IsNotInUseAndListsNoMapping(bool deleteTheEdition)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin, $"Exame {Marker()}");
        var edition = await EditionAsync(admin, exam.Id, ThisYear);
        var math = await SubjectAsync(admin, "Matematica");
        var row = await NoticeSubjectAsync(admin, exam.Id, edition.Id, "Linha", Whole(math.Id));

        var delete = deleteTheEdition
            ? await admin.DeleteAsync($"{Exams}/{exam.Id}/editions/{edition.Id}")
            : await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{row.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent, await delete.Content.ReadAsStringAsync());

        var subject = await admin.GetFromJsonAsync<SubjectResponse>($"{Subjects}/{math.Id}", AppJson.Options);
        var whereUsed = await WhereUsedAsync(admin, $"{Subjects}/{math.Id}/where-used");

        subject!.InUse.Should().BeFalse();
        whereUsed.Mappings.Should().BeEmpty();
        whereUsed.QuestionCount.Should().Be(0);
    }

    // AC3, BR2: a subject whose topics are mapped, but which no notice subject maps whole, lists no mapping.
    [Fact]
    public async Task Subject_WhoseTopicsAreMappedButNotItself_ListsNoMapping()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin, $"Exame {Marker()}");
        var edition = await EditionAsync(admin, exam.Id, ThisYear);
        var portuguese = await SubjectAsync(admin, "Portugues");
        var crase = await TopicAsync(admin, portuguese.Id, "Crase");
        await NoticeSubjectAsync(admin, exam.Id, edition.Id, "Lingua Portuguesa", Topic(crase.Id));

        var whereUsed = await WhereUsedAsync(admin, $"{Subjects}/{portuguese.Id}/where-used");

        whereUsed.Mappings.Should().BeEmpty();
    }

    // AC4 (mapping half), BR3, BR4, BR5: a topic lists the live notice subjects that map it; a deleted row is left out.
    [Fact]
    public async Task Topic_MappedByLiveAndDeletedNoticeSubjects_ListsOnlyTheLiveOnes()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin, $"Guarda {Marker()}");
        var edition = await EditionAsync(admin, exam.Id, ThisYear);
        var portuguese = await SubjectAsync(admin, "Portugues");
        var crase = await TopicAsync(admin, portuguese.Id, "Crase");
        var live = await NoticeSubjectAsync(admin, exam.Id, edition.Id, "Lingua Portuguesa", Topic(crase.Id));
        var gone = await NoticeSubjectAsync(admin, exam.Id, edition.Id, "Redacao", Topic(crase.Id));
        (await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var whereUsed = await WhereUsedAsync(admin, $"{Topics}/{crase.Id}/where-used");

        whereUsed.Mappings.Should().Equal(
            new WhereUsedMappingResponse(exam.Id, exam.Name, edition.Id, ThisYear, null, live.Id, "Lingua Portuguesa"));
        whereUsed.QuestionCount.Should().Be(0);
    }

    // AC11, BR11: an unknown or deleted subject answers 404 subject.not_found.
    [Fact]
    public async Task Subject_UnknownOrDeleted_Answers404SubjectNotFound()
    {
        var admin = await AdminAsync();
        var deleted = await SubjectAsync(admin, "Apagada");
        (await admin.DeleteAsync($"{Subjects}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await admin.GetAsync($"{Subjects}/{id}/where-used");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.SubjectNotFound);
        }
    }

    // AC11, BR11: an unknown or deleted topic answers 404 topic.not_found.
    [Fact]
    public async Task Topic_UnknownOrDeleted_Answers404TopicNotFound()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, "Portugues");
        var deleted = await TopicAsync(admin, subject.Id, "Apagado");
        (await admin.DeleteAsync($"{Topics}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await admin.GetAsync($"{Topics}/{id}/where-used");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.TopicNotFound);
        }
    }

    // AC10, BR10: both endpoints need catalog.manage; a Student gets 403 identity.forbidden.
    [Fact]
    public async Task BothEndpoints_ForAStudent_Answer403Forbidden()
    {
        var admin = await AdminAsync();
        var subject = await SubjectAsync(admin, "Portugues");
        var topic = await TopicAsync(admin, subject.Id, "Crase");
        var student = await StudentAsync();

        foreach (var path in new[] { $"{Subjects}/{subject.Id}/where-used", $"{Topics}/{topic.Id}/where-used" })
        {
            var response = await student.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeOfAsync(response)).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }
}
