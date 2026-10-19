using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-86 through HTTP, as the Web calls it: the copy sources of an edition and the copy of another edition's notice
/// subjects, with their mapping, into an empty one. The tests share one database, so each one works on its own
/// exam, boards and editions and never asserts a global count.
/// </summary>
public sealed class CopyNoticeSubjectsEndpointTests : CatalogApiTests
{
    private const string Catalog = "/api/v1/catalog";
    private const string Exams = Catalog + "/exams";
    private const string Subjects = Catalog + "/subjects";

    private static readonly int ThisYear = TimeProvider.System.GetUtcNow().Year;

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static string SubjectsOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-subjects";

    private static string CopyOf(Guid exam, Guid edition) => SubjectsOf(exam, edition) + "/copy";

    private static string SourcesOf(Guid exam, Guid edition) => SubjectsOf(exam, edition) + "/copy-sources";

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<T>(AppJson.Options))!;
    }

    private static async Task<Guid> NewExamAsync(HttpClient admin)
    {
        var authority = await ReadAsync<IssuingAuthorityResponse>(await admin.PostAsJsonAsync(
            Catalog + "/issuing-authorities",
            new SaveIssuingAuthorityRequest($"Orgao {Marker()}", Marker()),
            AppJson.Options));
        var exam = await ReadAsync<ExamResponse>(await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authority.Id, $"Exame {Marker()}", nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options));

        return exam.Id;
    }

    private static async Task<Guid> NewEditionAsync(HttpClient admin, Guid exam, int yearsAgo = 0, string? status = null)
    {
        var board = await ReadAsync<OrganizerResponse>(await admin.PostAsJsonAsync(
            Catalog + "/organizers",
            new SaveOrganizerRequest($"Banca {Marker()}", Marker(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options));
        var edition = await ReadAsync<ExamEditionResponse>(await admin.PostAsJsonAsync(
            $"{Exams}/{exam}/editions",
            new SaveExamEditionRequest(board.Id, ThisYear - yearsAgo, null, null, null, null, status),
            AppJson.Options));

        return edition.Id;
    }

    private static async Task<NoticeSubjectResponse> AddAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string? group,
        string label,
        int? count = null,
        params NoticeSubjectMappingRequest[] mappings) =>
        await ReadAsync<NoticeSubjectResponse>(await admin.PostAsJsonAsync(
            SubjectsOf(exam, edition),
            new SaveNoticeSubjectRequest(group, label, count, mappings),
            AppJson.Options));

    private static async Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(HttpClient admin, Guid exam, Guid edition) =>
        (await admin.GetFromJsonAsync<List<NoticeSubjectResponse>>(SubjectsOf(exam, edition), AppJson.Options))!;

    private static async Task<IReadOnlyList<NoticeSubjectCopySourceResponse>> SourcesAsync(HttpClient admin, Guid exam, Guid edition) =>
        (await admin.GetFromJsonAsync<List<NoticeSubjectCopySourceResponse>>(SourcesOf(exam, edition), AppJson.Options))!;

    private static Task<HttpResponseMessage> CopyAsync(HttpClient admin, Guid exam, Guid target, Guid? source) =>
        admin.PostAsJsonAsync(CopyOf(exam, target), new CopyNoticeSubjectsRequest(source), AppJson.Options);

    private static async Task DeleteSubjectAsync(HttpClient admin, Guid exam, Guid edition, Guid id)
    {
        var response = await admin.DeleteAsync($"{SubjectsOf(exam, edition)}/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> CodeOfAsync(HttpResponseMessage response) =>
        CodeOf(await response.Content.ReadAsStringAsync());

    private static async Task<SubjectResponse> CanonicalSubjectAsync(HttpClient admin, string name) =>
        await ReadAsync<SubjectResponse>(await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest($"{name} {Marker()}"), AppJson.Options));

    private static async Task<TopicResponse> CanonicalTopicAsync(HttpClient admin, Guid subjectId, string name) =>
        await ReadAsync<TopicResponse>(await admin.PostAsJsonAsync($"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options));

    // AC1: the live rows of the source, in its order, are new rows of the target; the source is not changed.
    [Fact]
    public async Task Copy_IntoAnEmptyEdition_CopiesTheLiveRowsInOrderAndLeavesTheSourceAlone()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 2);
        var target = await NewEditionAsync(admin, exam);
        await AddAsync(admin, exam, source, "Basicos", "Portugues", 10);
        await AddAsync(admin, exam, source, "Especificos", "Direito", 20);
        await AddAsync(admin, exam, source, "Basicos", "Matematica");
        var removed = await AddAsync(admin, exam, source, "Especificos", "Informatica", 5);
        await DeleteSubjectAsync(admin, exam, source, removed.Id);
        var before = await ListAsync(admin, exam, source);

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadAsync<CopyNoticeSubjectsResponse>(response)).CopiedCount.Should().Be(3);
        var copies = await ListAsync(admin, exam, target);
        copies.Select(row => (row.Group, row.Label, row.QuestionCount))
            .Should().Equal(before.Select(row => (row.Group, row.Label, row.QuestionCount)));
        copies.Select(row => row.Id).Should().NotIntersectWith(before.Select(row => row.Id));
        copies.Should().OnlyContain(row => row.ExamEditionId == target);
        (await ListAsync(admin, exam, source)).Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
        (await QueryAsync(context => context.NoticeSubjects
                .Where(row => row.ExamEditionId == target)
                .Select(row => row.TenantId)
                .ToListAsync()))
            .Should().OnlyContain(tenant => tenant == null);
    }

    // AC2: each copy carries the entries of its source row, whole subjects and topics alike.
    [Fact]
    public async Task Copy_CarriesTheMappingOfEachRow()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1);
        var target = await NewEditionAsync(admin, exam);
        var math = await CanonicalSubjectAsync(admin, "Matematica");
        var logic = await CanonicalSubjectAsync(admin, "Logica");
        var propositions = await CanonicalTopicAsync(admin, logic.Id, "Proposicoes");
        await AddAsync(admin, exam, source, null, "Matematica e logica", 10, new NoticeSubjectMappingRequest(SubjectId: math.Id), new NoticeSubjectMappingRequest(TopicId: propositions.Id));
        await AddAsync(admin, exam, source, null, "Sem mapeamento");

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var copies = await ListAsync(admin, exam, target);
        copies.Single(row => row.Label == "Matematica e logica").Mappings.Should().BeEquivalentTo(
        [
            new NoticeSubjectMappingResponse(math.Id, math.Name, null, null),
            new NoticeSubjectMappingResponse(logic.Id, logic.Name, propositions.Id, "Proposicoes")
        ]);
        copies.Single(row => row.Label == "Sem mapeamento").Mappings.Should().BeNullOrEmpty();
    }

    // AC3: a live row in the target closes the copy and nothing is written.
    [Fact]
    public async Task Copy_IntoAnEditionWithALiveRow_IsRefusedAndWritesNothing()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1);
        var target = await NewEditionAsync(admin, exam);
        await AddAsync(admin, exam, source, null, "Portugues");
        await AddAsync(admin, exam, source, null, "Direito");
        await AddAsync(admin, exam, target, null, "Ja digitada");

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty);
        (await ListAsync(admin, exam, target)).Select(row => row.Label).Should().Equal("Ja digitada");
    }

    // AC4: rows the admin already deleted do not count as content.
    [Fact]
    public async Task Copy_IntoAnEditionWithOnlyDeletedRows_Succeeds()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1);
        var target = await NewEditionAsync(admin, exam);
        await AddAsync(admin, exam, source, null, "Portugues");
        var typed = await AddAsync(admin, exam, target, null, "Portugues");
        await DeleteSubjectAsync(admin, exam, target, typed.Id);

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ListAsync(admin, exam, target)).Select(row => row.Label).Should().Equal("Portugues");
    }

    // AC5: a source that is not an edition of this exam, is gone, or is the target itself is not found.
    [Fact]
    public async Task Copy_FromASourceThatCannotBeUsed_IsNotFound()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var target = await NewEditionAsync(admin, exam);
        var deleted = await NewEditionAsync(admin, exam, yearsAgo: 1);
        await AddAsync(admin, exam, deleted, null, "Portugues");
        var gone = await admin.DeleteAsync($"{Exams}/{exam}/editions/{deleted}");
        gone.IsSuccessStatusCode.Should().BeTrue(await gone.Content.ReadAsStringAsync());
        var otherExam = await NewExamAsync(admin);
        var foreign = await NewEditionAsync(admin, otherExam, yearsAgo: 1);
        await AddAsync(admin, otherExam, foreign, null, "Portugues");
        await AddAsync(admin, exam, target, null, "Ja digitada");

        var sources = new Guid?[] { deleted, foreign, Guid.CreateVersion7(), target, null };

        foreach (var source in sources)
        {
            var response = await CopyAsync(admin, exam, target, source);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"source {source}");
            (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.ExamEditionCopySourceNotFound, $"source {source}");
        }
    }

    // AC6: a source with nothing to copy (here, only a deleted row) is refused.
    [Fact]
    public async Task Copy_FromASourceWithNoLiveRow_IsRefused()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1);
        var target = await NewEditionAsync(admin, exam);
        var removed = await AddAsync(admin, exam, source, null, "Portugues");
        await DeleteSubjectAsync(admin, exam, source, removed.Id);

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeOfAsync(response)).Should().Be(CatalogErrorCodes.NoticeSubjectCopySourceEmpty);
        (await ListAsync(admin, exam, target)).Should().BeEmpty();
    }

    // AC7: when the save fails (another writer committed a row of the same label and group between the check and the
    // save), no copy and no mapping entry is left in the target.
    [Fact]
    public async Task Copy_WhenTheSaveFails_LeavesNeitherRowsNorMappingEntries()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1);
        var target = await NewEditionAsync(admin, exam);
        var math = await CanonicalSubjectAsync(admin, "Matematica");
        await AddAsync(admin, exam, source, "Basicos", "Portugues", 10, new NoticeSubjectMappingRequest(SubjectId: math.Id));
        await AddAsync(admin, exam, source, "Basicos", "Matematica", 10, new NoticeSubjectMappingRequest(SubjectId: math.Id));

        await using var scope = Factory.Services.CreateAsyncScope();
        var racing = new RacingStore(
            scope.ServiceProvider.GetRequiredService<INoticeSubjectStore>(),
            () => AddAsync(admin, exam, target, "Basicos", "Matematica"));
        var handler = new CopyNoticeSubjectsHandler(racing, scope.ServiceProvider.GetRequiredService<IExamEditionStore>());

        var result = await handler.HandleAsync(exam, target, new CopyNoticeSubjectsRequest(source));

        racing.Raced.Should().BeTrue("the copy must go through the store's save");
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(CatalogErrorCodes.NoticeSubjectCopyTargetNotEmpty);
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        (await ListAsync(admin, exam, target)).Select(row => row.Label).Should().Equal("Matematica");
        (await QueryAsync(context => context.NoticeSubjectMappings
                .CountAsync(mapping => context.NoticeSubjects.Any(row => row.ExamEditionId == target && row.Id == mapping.NoticeSubjectId))))
            .Should().Be(0);
    }

    // AC8: only the editions BR1 allows, with their live count, newest year first.
    [Fact]
    public async Task CopySources_ListsOnlyUsableEditionsNewestYearFirst()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var target = await NewEditionAsync(admin, exam);
        var recent = await NewEditionAsync(admin, exam, yearsAgo: 1, status: nameof(ExamEditionStatus.Published));
        var older = await NewEditionAsync(admin, exam, yearsAgo: 3);
        var empty = await NewEditionAsync(admin, exam, yearsAgo: 2);
        var deleted = await NewEditionAsync(admin, exam, yearsAgo: 4);
        var otherExam = await NewExamAsync(admin);
        var foreign = await NewEditionAsync(admin, otherExam, yearsAgo: 1);
        await AddAsync(admin, exam, recent, null, "Portugues");
        await AddAsync(admin, exam, recent, null, "Direito");
        var removed = await AddAsync(admin, exam, recent, null, "Informatica");
        await DeleteSubjectAsync(admin, exam, recent, removed.Id);
        await AddAsync(admin, exam, older, null, "Portugues");
        var onlyDeleted = await AddAsync(admin, exam, empty, null, "Portugues");
        await DeleteSubjectAsync(admin, exam, empty, onlyDeleted.Id);
        await AddAsync(admin, exam, deleted, null, "Portugues");
        await AddAsync(admin, otherExam, foreign, null, "Portugues");
        await AddAsync(admin, exam, target, null, "Da propria edicao");
        (await admin.DeleteAsync($"{Exams}/{exam}/editions/{deleted}")).IsSuccessStatusCode.Should().BeTrue();

        var sources = await SourcesAsync(admin, exam, target);

        sources.Select(source => (source.EditionId, source.NoticeYear, source.NoticeSubjectCount))
            .Should().Equal((recent, ThisYear - 1, 2), (older, ThisYear - 3, 1));
        sources.Should().OnlyContain(source => !string.IsNullOrEmpty(source.OrganizerName));
    }

    // AC9: a published target takes the copy like a draft one (a published source is listed above).
    [Fact]
    public async Task Copy_IntoAPublishedEdition_Succeeds()
    {
        var admin = await AdminAsync();
        var exam = await NewExamAsync(admin);
        var source = await NewEditionAsync(admin, exam, yearsAgo: 1, status: nameof(ExamEditionStatus.Published));
        var target = await NewEditionAsync(admin, exam, status: nameof(ExamEditionStatus.Published));
        await AddAsync(admin, exam, source, null, "Portugues");

        var response = await CopyAsync(admin, exam, target, source);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ListAsync(admin, exam, target)).Select(row => row.Label).Should().Equal("Portugues");
    }

    // AC10: without the permission both routes are closed.
    [Fact]
    public async Task BothRoutes_WithoutTheManagePermission_AreForbidden()
    {
        var student = await StudentAsync();
        var exam = Guid.CreateVersion7();
        var edition = Guid.CreateVersion7();

        var responses = new[]
        {
            await student.GetAsync(SourcesOf(exam, edition)),
            await student.PostAsJsonAsync(CopyOf(exam, edition), new CopyNoticeSubjectsRequest(Guid.CreateVersion7()), AppJson.Options)
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    /// <summary>Lets another writer commit a row just before the real save, as a second admin would.</summary>
    private sealed class RacingStore(INoticeSubjectStore inner, Func<Task> race) : INoticeSubjectStore
    {
        public bool Raced { get; private set; }

        public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
        {
            Raced = true;
            await race();

            return await inner.TrySaveChangesAsync(cancellationToken);
        }

        public Task<NoticeSubject?> FindAsync(Guid examEditionId, Guid id, CancellationToken cancellationToken) =>
            inner.FindAsync(examEditionId, id, cancellationToken);

        public Task<IReadOnlyList<NoticeSubject>> ListForEditionAsync(Guid examEditionId, CancellationToken cancellationToken) =>
            inner.ListForEditionAsync(examEditionId, cancellationToken);

        public Task<bool> IsDuplicateAsync(
            Guid examEditionId,
            string normalizedGroup,
            string normalizedLabel,
            Guid? exceptId,
            CancellationToken cancellationToken) =>
            inner.IsDuplicateAsync(examEditionId, normalizedGroup, normalizedLabel, exceptId, cancellationToken);

        public Task<IReadOnlyList<NoticeSubjectMapping>> ListMappingsAsync(Guid noticeSubjectId, CancellationToken cancellationToken) =>
            inner.ListMappingsAsync(noticeSubjectId, cancellationToken);

        public Task<IReadOnlyList<NoticeSubjectMapping>> ListMappingsOfEditionAsync(Guid examEditionId, CancellationToken cancellationToken) =>
            inner.ListMappingsOfEditionAsync(examEditionId, cancellationToken);

        public Task<MappingTargets> FindLiveTargetsAsync(
            IReadOnlyCollection<Guid> subjectIds,
            IReadOnlyCollection<Guid> topicIds,
            CancellationToken cancellationToken) =>
            inner.FindLiveTargetsAsync(subjectIds, topicIds, cancellationToken);

        public void AddMapping(NoticeSubjectMapping mapping) => inner.AddMapping(mapping);

        public void RemoveMapping(NoticeSubjectMapping mapping) => inner.RemoveMapping(mapping);

        public void Add(NoticeSubject subject) => inner.Add(subject);

        public void Remove(NoticeSubject subject) => inner.Remove(subject);
    }
}
