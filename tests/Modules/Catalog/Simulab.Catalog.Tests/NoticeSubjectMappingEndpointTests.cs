using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-75 through HTTP, as the Web calls it: saving a notice subject with its mapping, the taxonomy the picker
/// reads, and the in-use guards on the subject and topic deletes. The tests share one database, so each one
/// works on its own exam, edition, subjects and topics.
/// </summary>
public sealed class NoticeSubjectMappingEndpointTests : CatalogApiTests
{
    private const string Catalog = "/api/v1/catalog";
    private const string Exams = Catalog + "/exams";
    private const string Subjects = Catalog + "/subjects";
    private const string Topics = Catalog + "/topics";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static string SubjectsOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-subjects";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    private static async Task<(Guid Exam, Guid Edition)> NewEditionAsync(HttpClient admin, string? status = null)
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
        var edition = await ReadAsync<ExamEditionResponse>(await admin.PostAsJsonAsync(
            $"{Exams}/{exam.Id}/editions",
            new SaveExamEditionRequest(board.Id, TimeProvider.System.GetUtcNow().Year, null, null, null, null, status),
            AppJson.Options));

        return (exam.Id, edition.Id);
    }

    private static async Task<SubjectResponse> SubjectAsync(HttpClient admin, string name) =>
        await ReadAsync<SubjectResponse>(await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"{name} {Marker()}"), AppJson.Options));

    private static async Task<TopicResponse> TopicAsync(HttpClient admin, Guid subjectId, string name) =>
        await ReadAsync<TopicResponse>(await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options));

    private static Task<HttpResponseMessage> SaveAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        IReadOnlyList<NoticeSubjectMappingRequest>? mappings,
        Guid? id = null)
    {
        var body = new SaveNoticeSubjectRequest(null, label, null, mappings);

        return id is { } existing
            ? admin.PutAsJsonAsync($"{SubjectsOf(exam, edition)}/{existing}", body, AppJson.Options)
            : admin.PostAsJsonAsync(SubjectsOf(exam, edition), body, AppJson.Options);
    }

    private static async Task<NoticeSubjectResponse> CreateAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        params NoticeSubjectMappingRequest[] mappings) =>
        await ReadAsync<NoticeSubjectResponse>(await SaveAsync(admin, exam, edition, label, mappings));

    private static async Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(HttpClient admin, Guid exam, Guid edition) =>
        (await admin.GetFromJsonAsync<List<NoticeSubjectResponse>>(SubjectsOf(exam, edition), AppJson.Options))!;

    private static NoticeSubjectMappingRequest Whole(Guid id) => new(SubjectId: id);

    private static NoticeSubjectMappingRequest Topic(Guid id) => new(TopicId: id);

    private static async Task<string> CodeOfAsync(HttpResponseMessage response) =>
        CodeOf(await response.Content.ReadAsStringAsync());

    // AC1
    [Fact]
    public async Task Save_WholeSubjectAndATopicOfAnother_StoresBothAndTheListShowsTheNames()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var logic = await SubjectAsync(admin, "Raciocinio Logico");
        var propositions = await TopicAsync(admin, logic.Id, "Proposicoes");

        var created = await CreateAsync(admin, exam, edition, "Raciocinio Logico-Matematico", Whole(math.Id), Topic(propositions.Id));

        created.Mappings.Should().BeEquivalentTo(
        [
            new NoticeSubjectMappingResponse(math.Id, math.Name, null, null),
            new NoticeSubjectMappingResponse(logic.Id, logic.Name, propositions.Id, "Proposicoes")
        ]);
        (await ListAsync(admin, exam, edition)).Single().Mappings.Should().BeEquivalentTo(created.Mappings);
    }

    // AC2: nothing changes when the body is refused.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Save_EntryWithBothIdsOrNeither_IsRefusedAndNothingChanges(bool both)
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");
        var row = await CreateAsync(admin, exam, edition, "Matematica do edital", Whole(math.Id));

        var response = await SaveAsync(
            admin,
            exam,
            edition,
            "Matematica do edital",
            [both ? new NoticeSubjectMappingRequest(math.Id, topic.Id) : new NoticeSubjectMappingRequest()],
            row.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.NoticeSubjectMappingInvalid);
        (await ListAsync(admin, exam, edition)).Single().Mappings.Should().ContainSingle().Which.SubjectId.Should().Be(math.Id);
    }

    // AC3
    [Fact]
    public async Task Save_FiftyOneEntries_IsRefused()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var entries = Enumerable.Range(0, CatalogLimits.NoticeSubjectMappingMax + 1).Select(_ => Whole(Guid.CreateVersion7())).ToList();

        var response = await SaveAsync(admin, exam, edition, "Muitas", entries);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.NoticeSubjectMappingTooMany);
    }

    // AC4: an unknown id, and a deleted subject and topic, are all refused.
    [Fact]
    public async Task Save_UnknownOrDeletedTarget_IsRefused()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var deletedSubject = await SubjectAsync(admin, "Apagada");
        var holder = await SubjectAsync(admin, "Com topico");
        var deletedTopic = await TopicAsync(admin, holder.Id, "Apagado");
        (await admin.DeleteAsync($"{Subjects}/{deletedSubject.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Topics}/{deletedTopic.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var targets = new[] { Whole(Guid.CreateVersion7()), Topic(Guid.CreateVersion7()), Whole(deletedSubject.Id), Topic(deletedTopic.Id) };

        foreach (var target in targets)
        {
            var response = await SaveAsync(admin, exam, edition, $"Linha {Marker()}", [target]);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.NoticeSubjectMappingTargetNotFound);
        }

        (await ListAsync(admin, exam, edition)).Should().BeEmpty();
    }

    // AC5
    [Fact]
    public async Task Save_WholeSubjectWithOwnTopic_IsRefused_ButWithAnotherSubjectsTopicIsStored()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var fractions = await TopicAsync(admin, math.Id, "Fracoes");
        var logic = await SubjectAsync(admin, "Logica");
        var propositions = await TopicAsync(admin, logic.Id, "Proposicoes");

        var overlap = await SaveAsync(admin, exam, edition, "Sobreposta", [Whole(math.Id), Topic(fractions.Id)]);
        var fine = await SaveAsync(admin, exam, edition, "Valida", [Whole(math.Id), Topic(propositions.Id)]);

        overlap.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeOfAsync(overlap)).Should().Be(CatalogErrorCodes.NoticeSubjectMappingOverlap);
        fine.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListAsync(admin, exam, edition)).Select(row => row.Label).Should().Equal("Valida");
    }

    // AC6, BR8: a topic moved under a subject mapped whole shows under its new subject, and the row can still be saved.
    [Fact]
    public async Task MovingATopicIntoAMappedWholeSubject_SucceedsAndTheRowShowsBothEntriesUnderTheNewSubject()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var logic = await SubjectAsync(admin, "Logica");
        var topic = await TopicAsync(admin, logic.Id, "Proposicoes");
        var row = await CreateAsync(admin, exam, edition, "Raciocinio", Whole(math.Id), Topic(topic.Id));

        var move = await admin.PutAsJsonAsync($"{Topics}/{topic.Id}", new SaveTopicRequest("Proposicoes", math.Id), AppJson.Options);

        move.StatusCode.Should().Be(HttpStatusCode.OK, await move.Content.ReadAsStringAsync());
        var shown = (await ListAsync(admin, exam, edition)).Single();
        shown.Mappings.Should().BeEquivalentTo(
        [
            new NoticeSubjectMappingResponse(math.Id, math.Name, null, null),
            new NoticeSubjectMappingResponse(math.Id, math.Name, topic.Id, "Proposicoes")
        ]);
        var again = await SaveAsync(
            admin,
            exam,
            edition,
            "Raciocinio",
            [Whole(math.Id), Topic(topic.Id)],
            row.Id);
        again.StatusCode.Should().Be(HttpStatusCode.OK, await again.Content.ReadAsStringAsync());
    }

    // AC7
    [Fact]
    public async Task Save_TheSameTopicTwice_IsStoredOnce()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");

        var created = await CreateAsync(admin, exam, edition, "Uma vez", Topic(topic.Id), Topic(topic.Id));

        created.Mappings.Should().ContainSingle();
        var stored = await QueryAsync(context => Task.FromResult(
            context.NoticeSubjectMappings.Count(mapping => mapping.NoticeSubjectId == created.Id)));
        stored.Should().Be(1);
    }

    // AC8
    [Fact]
    public async Task Save_TwoRowsOfOneEditionMappingTheSameTopic_BothAreStored()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");

        await CreateAsync(admin, exam, edition, "Primeira", Topic(topic.Id));
        await CreateAsync(admin, exam, edition, "Segunda", Topic(topic.Id));

        (await ListAsync(admin, exam, edition)).Should().OnlyContain(row => row.Mappings!.Count == 1);
    }

    // AC9, BR7: a save replaces the whole mapping, and a dropped entry can come back.
    [Fact]
    public async Task Save_ReplacesTheMapping_AnEmptyListClearsIt_AndADroppedEntryCanBeAddedAgain()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var a = await SubjectAsync(admin, "A");
        var b = await SubjectAsync(admin, "B");
        var c = await SubjectAsync(admin, "C");
        var row = await CreateAsync(admin, exam, edition, "Linha", Whole(a.Id), Whole(b.Id));

        var onlyC = await ReadAsync<NoticeSubjectResponse>(await SaveAsync(admin, exam, edition, "Linha", [Whole(c.Id)], row.Id));
        onlyC.Mappings.Should().ContainSingle().Which.SubjectId.Should().Be(c.Id);

        var backToA = await ReadAsync<NoticeSubjectResponse>(await SaveAsync(admin, exam, edition, "Linha", [Whole(a.Id)], row.Id));
        backToA.Mappings.Should().ContainSingle().Which.SubjectId.Should().Be(a.Id);

        var cleared = await ReadAsync<NoticeSubjectResponse>(await SaveAsync(admin, exam, edition, "Linha", [], row.Id));
        cleared.Mappings.Should().BeEmpty();
    }

    // BR7: a body that leaves "mappings" out clears the mapping.
    [Fact]
    public async Task Save_WithoutTheMappingsField_ClearsTheMapping()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var row = await CreateAsync(admin, exam, edition, "Linha", Whole(math.Id));

        var response = await SaveAsync(admin, exam, edition, "Linha", null, row.Id);

        (await ReadAsync<NoticeSubjectResponse>(response)).Mappings.Should().BeEmpty();
    }

    // AC10, BR2: an unmapped row is accepted, also in a published edition.
    [Fact]
    public async Task Save_WithoutAMapping_IsAcceptedEvenInAPublishedEdition()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin, nameof(ExamEditionStatus.Published));

        var row = await CreateAsync(admin, exam, edition, "Sem mapeamento");

        row.Mappings.Should().BeEmpty();
        (await ListAsync(admin, exam, edition)).Single().Mappings.Should().BeEmpty();
    }

    // AC11
    [Fact]
    public async Task Delete_ATopicAMappedRowUses_IsRefusedAndTheListSaysItIsInUse()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");
        await CreateAsync(admin, exam, edition, "Linha", Topic(topic.Id));

        var listed = await admin.GetFromJsonAsync<List<TopicResponse>>($"{Subjects}/{math.Id}/topics", AppJson.Options);
        var response = await admin.DeleteAsync($"{Topics}/{topic.Id}");

        listed!.Single().InUse.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task Delete_ASubjectMappedWhole_IsRefusedAndTheListSaysItIsInUse()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        await CreateAsync(admin, exam, edition, "Linha", Whole(math.Id));

        var found = await admin.GetFromJsonAsync<SubjectResponse>($"{Subjects}/{math.Id}", AppJson.Options);
        var response = await admin.DeleteAsync($"{Subjects}/{math.Id}");

        found!.InUse.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.SubjectInUse);
    }

    // BR9: a subject with topics is still refused first by subject.has_topics.
    [Fact]
    public async Task Delete_ASubjectWithTopicsThatIsAlsoMapped_IsRefusedForItsTopicsFirst()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        await TopicAsync(admin, math.Id, "Fracoes");
        await CreateAsync(admin, exam, edition, "Linha", Whole(math.Id));

        var response = await admin.DeleteAsync($"{Subjects}/{math.Id}");

        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.SubjectHasTopics);
    }

    // The saved answers carry the flag too.
    [Fact]
    public async Task SavingATopic_AnswersWithItsInUseFlag()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");
        topic.InUse.Should().BeFalse();
        await CreateAsync(admin, exam, edition, "Linha", Topic(topic.Id));

        var renamed = await ReadAsync<TopicResponse>(
            await admin.PutAsJsonAsync($"{Topics}/{topic.Id}", new SaveTopicRequest("Fracoes e decimais"), AppJson.Options));

        renamed.InUse.Should().BeTrue();
    }

    // AC12, BR11: a mapping of a deleted notice subject or of a deleted edition no longer holds the topic.
    [Fact]
    public async Task Delete_ATopicMappedOnlyByADeletedNoticeSubject_IsAccepted()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");
        var row = await CreateAsync(admin, exam, edition, "Linha", Topic(topic.Id));
        (await admin.DeleteAsync($"{SubjectsOf(exam, edition)}/{row.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.DeleteAsync($"{Topics}/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ATopicMappedOnlyByARowOfADeletedEdition_IsAccepted()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var math = await SubjectAsync(admin, "Matematica");
        var topic = await TopicAsync(admin, math.Id, "Fracoes");
        await CreateAsync(admin, exam, edition, "Linha", Topic(topic.Id));
        (await admin.DeleteAsync($"{Exams}/{exam}/editions/{edition}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.DeleteAsync($"{Topics}/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // AC13
    [Fact]
    public async Task Taxonomy_ReturnsLiveSubjectsAndTopicsAlphabetically_AccentsIgnored_NoDeletedItem()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var tree = await SubjectAsync(admin, $"Arvore {marker}");
        var accented = await ReadAsync<SubjectResponse>(
            await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"Água {marker}"), AppJson.Options));
        var zebra = await SubjectAsync(admin, $"Zebra {marker}");
        await TopicAsync(admin, accented.Id, "Zeta");
        await TopicAsync(admin, accented.Id, "Beta");
        var gone = await TopicAsync(admin, accented.Id, "Alfa");
        (await admin.DeleteAsync($"{Topics}/{gone.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Subjects}/{zebra.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var taxonomy = (await admin.GetFromJsonAsync<List<TaxonomySubjectResponse>>(Catalog + "/taxonomy", AppJson.Options))!
            .Where(subject => subject.Name.EndsWith(marker, StringComparison.Ordinal))
            .ToList();

        taxonomy.Select(subject => subject.Name).Should().Equal(accented.Name, tree.Name);
        taxonomy[0].Topics.Select(topic => topic.Name).Should().Equal("Beta", "Zeta");
        taxonomy[1].Topics.Should().BeEmpty();
    }

    // AC14, BR12
    [Fact]
    public async Task TaxonomyAndMappingEndpoints_WithoutTheManagePermission_AreForbidden()
    {
        var student = await StudentAsync();
        var exam = Guid.CreateVersion7();
        var edition = Guid.CreateVersion7();
        var body = new SaveNoticeSubjectRequest(null, "Linha", null, [Whole(Guid.CreateVersion7())]);

        var responses = new[]
        {
            await student.GetAsync(Catalog + "/taxonomy"),
            await student.PostAsJsonAsync(SubjectsOf(exam, edition), body, AppJson.Options),
            await student.PutAsJsonAsync($"{SubjectsOf(exam, edition)}/{Guid.CreateVersion7()}", body, AppJson.Options)
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeOfAsync(response)).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }
}
