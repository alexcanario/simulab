using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-74 through HTTP, as the Web calls it. The tests share one database, so each one works on its own exam,
/// board and edition and never asserts a global count.
/// </summary>
public sealed class NoticeSubjectEndpointTests : CatalogApiTests
{
    private const string Exams = "/api/v1/catalog/exams";
    private const string Organizers = "/api/v1/catalog/organizers";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    private static readonly int ThisYear = TimeProvider.System.GetUtcNow().Year;

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static string SubjectsOf(Guid exam, Guid edition) => $"{Exams}/{exam}/editions/{edition}/notice-subjects";

    private static async Task<ExamResponse> ExamAsync(HttpClient admin)
    {
        var authority = await admin.PostAsJsonAsync(
            Authorities,
            new SaveIssuingAuthorityRequest(Unique("Orgao"), Guid.CreateVersion7().ToString("N")[..12]),
            AppJson.Options);
        authority.StatusCode.Should().Be(HttpStatusCode.Created, await authority.Content.ReadAsStringAsync());
        var authorityId = (await authority.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!.Id;

        var response = await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authorityId, Unique("Exame"), nameof(AssessmentType.PublicServiceExam), nameof(ExamScope.National), null, "pt-BR"),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
    }

    private static async Task<ExamEditionResponse> EditionAsync(
        HttpClient admin,
        Guid exam,
        string? status = null,
        Guid? board = null)
    {
        var boardId = board ?? await BoardAsync(admin);
        var response = await admin.PostAsJsonAsync(
            $"{Exams}/{exam}/editions",
            new SaveExamEditionRequest(boardId, ThisYear, null, null, null, null, status),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    private static async Task<Guid> BoardAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            Organizers,
            new SaveOrganizerRequest(Unique("Banca"), Guid.CreateVersion7().ToString("N")[..12], nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!.Id;
    }

    /// <summary>An exam with one Draft edition: the place every test adds its notice subjects.</summary>
    private static async Task<(ExamResponse Exam, ExamEditionResponse Edition)> NewEditionAsync(
        HttpClient admin,
        string? status = null)
    {
        var exam = await ExamAsync(admin);

        return (exam, await EditionAsync(admin, exam.Id, status));
    }

    private static Task<HttpResponseMessage> AddAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string? group,
        string? label,
        int? count = null) =>
        admin.PostAsJsonAsync(SubjectsOf(exam, edition), new SaveNoticeSubjectRequest(group, label, count), AppJson.Options);

    private static async Task<NoticeSubjectResponse> CreateAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string? group,
        string label,
        int? count = null)
    {
        var response = await AddAsync(admin, exam, edition, group, label, count);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<NoticeSubjectResponse>(AppJson.Options))!;
    }

    private static async Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(HttpClient admin, Guid exam, Guid edition) =>
        (await admin.GetFromJsonAsync<List<NoticeSubjectResponse>>(SubjectsOf(exam, edition), AppJson.Options))!;

    private static Task<HttpResponseMessage> MoveAsync(HttpClient admin, Guid exam, Guid edition, Guid id, string direction) =>
        admin.PostAsJsonAsync($"{SubjectsOf(exam, edition)}/{id}/move", new MoveNoticeSubjectRequest(direction), AppJson.Options);

    private static string[] Labels(IEnumerable<NoticeSubjectResponse> rows) => rows.Select(row => row.Label).ToArray();

    // AC16: without the permission every notice subject route is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var exam = Guid.CreateVersion7();
        var edition = Guid.CreateVersion7();
        var id = Guid.CreateVersion7();
        var body = new SaveNoticeSubjectRequest("Básicos", "Português", 10);

        var responses = new[]
        {
            await student.GetAsync(SubjectsOf(exam, edition)),
            await student.PostAsJsonAsync(SubjectsOf(exam, edition), body, AppJson.Options),
            await student.PutAsJsonAsync($"{SubjectsOf(exam, edition)}/{id}", body, AppJson.Options),
            await student.PostAsJsonAsync($"{SubjectsOf(exam, edition)}/{id}/move", new MoveNoticeSubjectRequest("up"), AppJson.Options),
            await student.DeleteAsync($"{SubjectsOf(exam, edition)}/{id}")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    // AC1: the list comes back with groups in the order of their first row and rows in their order.
    [Fact]
    public async Task List_ReturnsTheRowsGroupedInTheOrderOfTheirFirstRow()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "Básicos", "Português", 10);
        await CreateAsync(admin, exam.Id, edition.Id, "Específicos", "Direito", 20);
        await CreateAsync(admin, exam.Id, edition.Id, "Básicos", "Matemática");

        var rows = await ListAsync(admin, exam.Id, edition.Id);

        Labels(rows).Should().Equal("Português", "Matemática", "Direito");
        rows.Select(row => row.Group).Should().Equal("Básicos", "Básicos", "Específicos");
        rows.Select(row => row.QuestionCount).Should().Equal(10, null, 20);
        rows.Should().OnlyContain(row => row.ExamEditionId == edition.Id);
    }

    // AC3: the row is stored and appears last in its group.
    [Fact]
    public async Task Create_AddsTheRowLastInItsGroup()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "Basic knowledge", "Math");
        await CreateAsync(admin, exam.Id, edition.Id, "Specific", "Law");

        var response = await AddAsync(admin, exam.Id, edition.Id, "Basic knowledge", "Portuguese", 10);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith($"/notice-subjects/{(await response.Content.ReadFromJsonAsync<NoticeSubjectResponse>(AppJson.Options))!.Id}");
        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Math", "Portuguese", "Law");
    }

    // AC4: each label problem has its own code and nothing is stored.
    [Fact]
    public async Task Create_InvalidLabel_IsRefusedWithItsOwnCode()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);

        var blank = await AddAsync(admin, exam.Id, edition.Id, null, "   ");
        var tooShort = await AddAsync(admin, exam.Id, edition.Id, null, "A");
        var tooLong = await AddAsync(admin, exam.Id, edition.Id, null, new string('x', CatalogLimits.NoticeSubjectLabelMaxLength + 1));

        foreach (var (response, code) in new[]
        {
            (blank, CatalogErrorCodes.NoticeSubjectLabelRequired),
            (tooShort, CatalogErrorCodes.NoticeSubjectLabelTooShort),
            (tooLong, CatalogErrorCodes.NoticeSubjectLabelTooLong)
        })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        }

        (await ListAsync(admin, exam.Id, edition.Id)).Should().BeEmpty();
    }

    // AC5: a 101-character group is refused; a blank group is stored as null.
    [Fact]
    public async Task Create_GroupTooLong_IsRefused_AndABlankGroupIsStoredAsNull()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);

        var tooLong = await AddAsync(admin, exam.Id, edition.Id, new string('g', CatalogLimits.NoticeSubjectGroupMaxLength + 1), "Português");
        var blank = await AddAsync(admin, exam.Id, edition.Id, "   ", "Português");

        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectGroupTooLong);
        blank.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListAsync(admin, exam.Id, edition.Id)).Should().ContainSingle().Which.Group.Should().BeNull();
    }

    // AC6: 0, 501 and a negative number are refused; none is stored as null.
    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    [InlineData(-4)]
    public async Task Create_QuestionCountOutsideTheRange_IsRefused(int count)
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);

        var response = await AddAsync(admin, exam.Id, edition.Id, null, "Português", count);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectQuestionCountInvalid);
        (await ListAsync(admin, exam.Id, edition.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithoutAQuestionCount_StoresNull()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);

        await CreateAsync(admin, exam.Id, edition.Id, null, "Português");

        (await ListAsync(admin, exam.Id, edition.Id)).Should().ContainSingle().Which.QuestionCount.Should().BeNull();
    }

    // AC7: the label is unique inside its group ignoring case and accents; another group may repeat it.
    [Fact]
    public async Task Create_SameLabelInTheSameGroup_IsADuplicate_ButFreeInAnotherGroup()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "Básicos", "Português");

        var duplicate = await AddAsync(admin, exam.Id, edition.Id, "básicos", "portugues");
        var elsewhere = await AddAsync(admin, exam.Id, edition.Id, "Específicos", "portugues");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await duplicate.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectDuplicate);
        elsewhere.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_SameLabelWithNoGroupTwice_IsADuplicate()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, null, "Português");

        var duplicate = await AddAsync(admin, exam.Id, edition.Id, "  ", "PORTUGUÊS");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_SameLabelInAnotherEdition_IsAccepted()
    {
        var admin = await AdminAsync();
        var (exam, first) = await NewEditionAsync(admin);
        var second = await EditionAsync(admin, exam.Id);
        await CreateAsync(admin, exam.Id, first.Id, "Básicos", "Português");

        var response = await AddAsync(admin, exam.Id, second.Id, "Básicos", "Português");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // AC8: a deleted label does not hold its place in the group.
    [Fact]
    public async Task Create_ALabelThatWasDeleted_CanBeAddedAgain()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var first = await CreateAsync(admin, exam.Id, edition.Id, "Básicos", "Português");
        (await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{first.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var again = await AddAsync(admin, exam.Id, edition.Id, "Básicos", "Português");

        again.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListAsync(admin, exam.Id, edition.Id)).Should().ContainSingle();
    }

    // AC9: an edit that keeps the group keeps the position.
    [Fact]
    public async Task Update_ChangesTheLabelAndTheNumber_AndKeepsThePosition()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha", 5);
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo", 5);
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Charlie", 5);

        var response = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam.Id, edition.Id)}/{b.Id}",
            new SaveNoticeSubjectRequest("G", "B renamed", 12),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ListAsync(admin, exam.Id, edition.Id);
        Labels(rows).Should().Equal("Alpha", "B renamed", "Charlie");
        rows[1].QuestionCount.Should().Be(12);
    }

    [Fact]
    public async Task Update_KeepingItsOwnLabel_IsNotADuplicateOfItself()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var row = await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha", 5);

        var response = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam.Id, edition.Id)}/{row.Id}",
            new SaveNoticeSubjectRequest("g", "alpha", 6),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ToALabelAnotherRowHasInTheGroup_IsADuplicate()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");

        var response = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam.Id, edition.Id)}/{b.Id}",
            new SaveNoticeSubjectRequest("G", "alpha", null),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectDuplicate);
    }

    // AC10: a row moved to another group appears last in the new group.
    [Fact]
    public async Task Update_ChangingTheGroup_MovesTheRowToTheEndOfTheNewGroup()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var a1 = await CreateAsync(admin, exam.Id, edition.Id, "G1", "A1");
        await CreateAsync(admin, exam.Id, edition.Id, "G1", "A2");
        await CreateAsync(admin, exam.Id, edition.Id, "G2", "B1");
        await CreateAsync(admin, exam.Id, edition.Id, "G2", "B2");

        var response = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam.Id, edition.Id)}/{a1.Id}",
            new SaveNoticeSubjectRequest("G2", "A1", null),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ListAsync(admin, exam.Id, edition.Id);
        Labels(rows).Should().Equal("A2", "B1", "B2", "A1");
        rows[^1].Group.Should().Be("G2");
    }

    // AC11: moving B up gives B, A, C; the edges are refused and nothing changes.
    [Fact]
    public async Task Move_SwapsWithTheNeighbourInTheGroup_AndRefusesTheEdges()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var a = await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");
        var c = await CreateAsync(admin, exam.Id, edition.Id, "G", "Charlie");

        (await MoveAsync(admin, exam.Id, edition.Id, b.Id, "up")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Bravo", "Alpha", "Charlie");

        var first = await MoveAsync(admin, exam.Id, edition.Id, b.Id, "UP");
        var last = await MoveAsync(admin, exam.Id, edition.Id, c.Id, "down");

        foreach (var response in new[] { first, last })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectMoveInvalid);
        }

        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Bravo", "Alpha", "Charlie");
        (await MoveAsync(admin, exam.Id, edition.Id, a.Id, "down")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Bravo", "Charlie", "Alpha");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sideways")]
    [InlineData("1")]
    public async Task Move_AnUnknownOrMissingDirection_IsMoveInvalid(string? direction)
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");

        var response = await MoveAsync(admin, exam.Id, edition.Id, b.Id, direction!);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectMoveInvalid);
    }

    // BR6 through HTTP: a row never crosses into the next group.
    [Fact]
    public async Task Move_LastRowOfAGroupDown_DoesNotCrossIntoTheNextGroup()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var a = await CreateAsync(admin, exam.Id, edition.Id, "G1", "Alpha");
        await CreateAsync(admin, exam.Id, edition.Id, "G2", "Bravo");

        var response = await MoveAsync(admin, exam.Id, edition.Id, a.Id, "down");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Alpha", "Bravo");
    }

    // AC12: a deleted row leaves the list and stays in the table, marked deleted.
    [Fact]
    public async Task Delete_HidesTheRowAndKeepsItMarkedDeleted()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var row = await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");

        (await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{row.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Bravo");
        var stored = await QueryAsync(context => context.NoticeSubjects
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(item => item.Id == row.Id));
        stored.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ThenMoveTheNeighbour_StillWorks()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");
        var c = await CreateAsync(admin, exam.Id, edition.Id, "G", "Charlie");
        (await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{b.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await MoveAsync(admin, exam.Id, edition.Id, c.Id, "up")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Charlie", "Alpha");
    }

    // AC13: deleting an edition soft-deletes its notice subjects in the same save.
    [Fact]
    public async Task DeleteEdition_SoftDeletesItsNoticeSubjects()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var other = await EditionAsync(admin, exam.Id);
        var row = await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var kept = await CreateAsync(admin, exam.Id, other.Id, "G", "Alpha");

        (await admin.DeleteAsync($"{Exams}/{exam.Id}/editions/{edition.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rows = await QueryAsync(context => context.NoticeSubjects
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .Where(item => item.Id == row.Id || item.Id == kept.Id)
            .ToListAsync());
        rows.Single(item => item.Id == row.Id).IsDeleted.Should().BeTrue();
        rows.Single(item => item.Id == row.Id).DeletedAt.Should().NotBeNull("the interceptor stamped the soft delete");
        rows.Single(item => item.Id == kept.Id).IsDeleted.Should().BeFalse("another edition's rows stay");
    }

    // AC14: a Published edition accepts every action.
    [Fact]
    public async Task APublishedEdition_AcceptsAddEditMoveAndDelete()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin, status: "Published");
        var a = await CreateAsync(admin, exam.Id, edition.Id, "G", "Alpha");
        var b = await CreateAsync(admin, exam.Id, edition.Id, "G", "Bravo");

        var edit = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam.Id, edition.Id)}/{a.Id}", new SaveNoticeSubjectRequest("G", "A2", 3), AppJson.Options);
        var move = await MoveAsync(admin, exam.Id, edition.Id, b.Id, "up");
        var delete = await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{a.Id}");

        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        move.StatusCode.Should().Be(HttpStatusCode.NoContent);
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Labels(await ListAsync(admin, exam.Id, edition.Id)).Should().Equal("Bravo");
    }

    // AC15: an edition that does not exist or belongs to another exam is exam_edition.not_found on every route.
    [Fact]
    public async Task AnEditionThatIsNotUnderTheExam_IsEditionNotFoundOnEveryRoute()
    {
        var admin = await AdminAsync();
        var (_, edition) = await NewEditionAsync(admin);
        var stranger = await ExamAsync(admin);
        var id = Guid.CreateVersion7();
        var body = new SaveNoticeSubjectRequest("G", "Alpha", 1);

        var responses = new[]
        {
            await admin.GetAsync(SubjectsOf(stranger.Id, edition.Id)),
            await admin.PostAsJsonAsync(SubjectsOf(stranger.Id, edition.Id), body, AppJson.Options),
            await admin.PutAsJsonAsync($"{SubjectsOf(stranger.Id, edition.Id)}/{id}", body, AppJson.Options),
            await admin.PostAsJsonAsync($"{SubjectsOf(stranger.Id, edition.Id)}/{id}/move", new MoveNoticeSubjectRequest("up"), AppJson.Options),
            await admin.DeleteAsync($"{SubjectsOf(stranger.Id, edition.Id)}/{id}"),
            await admin.GetAsync(SubjectsOf(Guid.CreateVersion7(), Guid.CreateVersion7()))
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNotFound);
        }
    }

    // AC15: a notice subject id of another edition is notice_subject.not_found.
    [Fact]
    public async Task ANoticeSubjectOfAnotherEdition_IsNoticeSubjectNotFound()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);
        var other = await EditionAsync(admin, exam.Id);
        var row = await CreateAsync(admin, exam.Id, other.Id, "G", "Alpha");
        var body = new SaveNoticeSubjectRequest("G", "Alpha", 1);

        var responses = new[]
        {
            await admin.PutAsJsonAsync($"{SubjectsOf(exam.Id, edition.Id)}/{row.Id}", body, AppJson.Options),
            await MoveAsync(admin, exam.Id, edition.Id, row.Id, "up"),
            await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{row.Id}"),
            await admin.DeleteAsync($"{SubjectsOf(exam.Id, edition.Id)}/{Guid.CreateVersion7()}")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectNotFound);
        }

        Labels(await ListAsync(admin, exam.Id, other.Id)).Should().Equal("Alpha");
    }

    // The same label added twice at once: the unique index answers the loser with the 409, never a 500.
    [Fact]
    public async Task TwoAddsOfTheSameLabelAtOnce_OneWins_AndTheOtherIsADuplicate()
    {
        var admin = await AdminAsync();
        var (exam, edition) = await NewEditionAsync(admin);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => AddAsync(admin, exam.Id, edition.Id, "G", "Same")));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Where(response => response.StatusCode != HttpStatusCode.Created)
            .Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Conflict);
        (await ListAsync(admin, exam.Id, edition.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task OpenApiDocument_DescribesTheNoticeSubjectRoutes()
    {
        var document = await Client().GetStringAsync("/openapi/v1.json");

        document.Should().Contain("/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects");
        document.Should().Contain("/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}/move");
    }
}
