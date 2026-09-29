using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-35 through HTTP, as the Web calls it. The tests of this class share one database, so each one works on
/// its own exam and its own boards and never asserts a global count.
/// </summary>
public sealed class ExamEditionEndpointTests : CatalogApiTests
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

    private static string EditionsOf(Guid exam) => $"{Exams}/{exam}/editions";

    private static SaveExamEditionRequest Valid(
        Guid board,
        int? year = null,
        string? position = null,
        string? status = null,
        string? reference = null,
        string? url = null,
        DateOnly? appliedOn = null) =>
        new(board, year ?? ThisYear, position, reference, url, appliedOn, status);

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

    private static async Task<OrganizerResponse> BoardAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            Organizers,
            new SaveOrganizerRequest(Unique("Banca"), Guid.CreateVersion7().ToString("N")[..12], nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
    }

    private static async Task<ExamEditionResponse> CreateAsync(HttpClient admin, Guid exam, SaveExamEditionRequest request)
    {
        var response = await admin.PostAsJsonAsync(EditionsOf(exam), request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    private static async Task<IReadOnlyList<ExamEditionResponse>> ListAsync(HttpClient admin, Guid exam) =>
        (await admin.GetFromJsonAsync<List<ExamEditionResponse>>(EditionsOf(exam), AppJson.Options))!;

    // AC16: without the permission every edition route is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var exam = Guid.CreateVersion7();
        var id = Guid.CreateVersion7();
        var body = Valid(Guid.CreateVersion7());

        var responses = new[]
        {
            await student.GetAsync(EditionsOf(exam)),
            await student.GetAsync($"{EditionsOf(exam)}/{id}"),
            await student.PostAsJsonAsync(EditionsOf(exam), body, AppJson.Options),
            await student.PutAsJsonAsync($"{EditionsOf(exam)}/{id}", body, AppJson.Options),
            await student.DeleteAsync($"{EditionsOf(exam)}/{id}")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    // AC2 and BR14: the list comes back in one call, newest year first, then position, then the board.
    [Fact]
    public async Task List_ReturnsTheEditionsNewestYearFirstThenPositionThenBoard()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2024, position: "Agente"));
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Zelador"));
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Ábaco"));
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Motorista"));

        var listed = await ListAsync(admin, exam.Id);

        listed.Select(item => (item.NoticeYear, item.Position)).Should().Equal(
            (2026, "Ábaco"),
            (2026, "Motorista"),
            (2026, "Zelador"),
            (2024, "Agente"));
        listed.Should().AllSatisfy(item =>
        {
            item.OrganizerAcronym.Should().Be(board.Acronym);
            item.OrganizerName.Should().Be(board.Name);
            item.ExamId.Should().Be(exam.Id);
        });
    }

    [Fact]
    public async Task List_OfAnExamThatDoesNotExist_IsExamNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync(EditionsOf(Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNotFound);
    }

    // AC4: a board and a year are enough; the edition is a draft and shows up in the exam's list.
    [Fact]
    public async Task Create_WithABoardAndAYearOnly_IsADraftAndAppearsInTheList()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var created = await CreateAsync(admin, exam.Id, Valid(board.Id));

        created.Status.Should().Be(ExamEditionStatus.Draft);
        created.Position.Should().BeNull();
        created.NoticeReference.Should().BeNull();
        created.NoticeUrl.Should().BeNull();
        created.AppliedOn.Should().BeNull();
        (await ListAsync(admin, exam.Id)).Should().ContainSingle(item => item.Id == created.Id);
    }

    // AC5: every field comes back as saved, the date on the same calendar day.
    [Fact]
    public async Task Create_WithEveryField_ComesBackAsSavedOnGetAndOnList()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var request = Valid(
            board.Id,
            year: 2026,
            position: "  Guarda Municipal de 3ª Classe  ",
            status: "Published",
            reference: "Edital nº 01/2026",
            url: "https://example.org/edital-01-2026.pdf",
            appliedOn: new DateOnly(2026, 6, 14));

        var created = await CreateAsync(admin, exam.Id, request);
        var found = (await admin.GetFromJsonAsync<ExamEditionResponse>($"{EditionsOf(exam.Id)}/{created.Id}", AppJson.Options))!;

        found.Should().Be(created);
        found.Position.Should().Be("Guarda Municipal de 3ª Classe");
        found.NoticeReference.Should().Be("Edital nº 01/2026");
        found.NoticeUrl.Should().Be("https://example.org/edital-01-2026.pdf");
        found.AppliedOn.Should().Be(new DateOnly(2026, 6, 14));
        found.Status.Should().Be(ExamEditionStatus.Published);
        found.OrganizerId.Should().Be(board.Id);

        var raw = await admin.GetStringAsync($"{EditionsOf(exam.Id)}/{created.Id}");
        using var document = JsonDocument.Parse(raw);
        document.RootElement.GetProperty("appliedOn").GetString().Should().Be("2026-06-14");
        document.RootElement.GetProperty("status").GetString().Should().Be("Published");
    }

    [Fact]
    public async Task Update_ReplacesTheFieldsAndKeepsTheExam()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var first = await BoardAsync(admin);
        var second = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam.Id, Valid(first.Id, year: 2025));

        var response = await admin.PutAsJsonAsync(
            $"{EditionsOf(exam.Id)}/{created.Id}",
            Valid(second.Id, year: 2026, position: "Inspetor", status: "Draft"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updated = (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
        updated.Id.Should().Be(created.Id);
        updated.ExamId.Should().Be(exam.Id);
        updated.OrganizerId.Should().Be(second.Id);
        updated.NoticeYear.Should().Be(2026);
        updated.Position.Should().Be("Inspetor");
    }

    // AC6: a year outside 1990 to next year is a 400 and nothing is written.
    [Theory]
    [InlineData(1989)]
    [InlineData(0)]
    public async Task Create_NoticeYearBelow1990_IsRefusedAndNothingIsWritten(int year)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, year: year), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeYearInvalid);
        (await ListAsync(admin, exam.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_NoticeYearTwoYearsAhead_IsRefusedAndNextYearIsAccepted()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var tooFar = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, year: ThisYear + 2), AppJson.Options);
        var nextYear = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, year: ThisYear + 1), AppJson.Options);

        tooFar.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooFar.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeYearInvalid);
        nextYear.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ListAsync(admin, exam.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithoutAYear_IsNoticeYearInvalid()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), new SaveExamEditionRequest(board.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeYearInvalid);
    }

    [Fact]
    public async Task Create_WithoutABoard_IsOrganizerRequired()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);

        var response = await admin.PostAsJsonAsync(
            EditionsOf(exam.Id),
            new SaveExamEditionRequest(NoticeYear: ThisYear),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionOrganizerRequired);
    }

    // AC7: the application date and the notice link are checked by the Api.
    [Fact]
    public async Task Create_AppliedOnBeforeTheNoticeYear_IsRefused()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(
            EditionsOf(exam.Id),
            Valid(board.Id, year: 2026, appliedOn: new DateOnly(2025, 12, 31)),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionAppliedOnBeforeNoticeYear);
    }

    [Theory]
    [InlineData("edital.pdf")]
    [InlineData("ftp://example.org/edital.pdf")]
    public async Task Create_NoticeUrlThatIsNotAnAbsoluteWebAddress_IsRefused(string url)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, url: url), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeUrlInvalid);
        (await ListAsync(admin, exam.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_PositionAndReferenceThatAreTooLong_AreRefusedWithTheirOwnCodes()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var position = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, position: new string('a', 201)), AppJson.Options);
        var reference = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, reference: new string('a', 101)), AppJson.Options);

        CodeOf(await position.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionPositionTooLong);
        CodeOf(await reference.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNoticeReferenceTooLong);
    }

    // AC8: the same year and board with a position that differs only in case or accents is a duplicate;
    // another board or another year is a different paper.
    [Theory]
    [InlineData("GUARDA MUNICIPAL")]
    [InlineData("guarda municipal")]
    [InlineData("Guárda Municipal")]
    public async Task Create_SameYearBoardAndPositionIgnoringCaseAndAccents_IsADuplicate(string duplicate)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Guarda Municipal"));

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, year: 2026, position: duplicate), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionDuplicate);
    }

    [Fact]
    public async Task Create_TwoEditionsWithNoPositionForTheSameYearAndBoard_IsADuplicate()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        await CreateAsync(admin, exam.Id, Valid(board.Id));

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionDuplicate);
    }

    [Fact]
    public async Task Create_AnotherBoardOrAnotherYear_IsCreated()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var other = await BoardAsync(admin);
        await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Guarda"));

        var anotherBoard = await CreateAsync(admin, exam.Id, Valid(other.Id, year: 2026, position: "Guarda"));
        var anotherYear = await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2025, position: "Guarda"));

        anotherBoard.OrganizerId.Should().Be(other.Id);
        anotherYear.NoticeYear.Should().Be(2025);
    }

    [Fact]
    public async Task Update_ToTheKeyOfAnotherEdition_IsADuplicateButKeepingItsOwnKeyIsNot()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var first = await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2026, position: "Guarda"));
        var second = await CreateAsync(admin, exam.Id, Valid(board.Id, year: 2025, position: "Guarda"));

        var collides = await admin.PutAsJsonAsync(
            $"{EditionsOf(exam.Id)}/{second.Id}",
            Valid(board.Id, year: 2026, position: "GUARDA", status: "Draft"),
            AppJson.Options);
        var keeps = await admin.PutAsJsonAsync(
            $"{EditionsOf(exam.Id)}/{first.Id}",
            Valid(board.Id, year: 2026, position: "GUARDA", status: "Draft", reference: "Edital 1"),
            AppJson.Options);

        collides.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await collides.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionDuplicate);
        keeps.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // BR10: the same paper may repeat under another exam.
    [Fact]
    public async Task Create_TheSameYearBoardAndPositionUnderAnotherExam_IsCreated()
    {
        var admin = await AdminAsync();
        var board = await BoardAsync(admin);
        var one = await ExamAsync(admin);
        var other = await ExamAsync(admin);

        await CreateAsync(admin, one.Id, Valid(board.Id, year: 2026, position: "Guarda"));
        var created = await CreateAsync(admin, other.Id, Valid(board.Id, year: 2026, position: "Guarda"));

        created.ExamId.Should().Be(other.Id);
    }

    // AC10: a parent that is not in the catalog, or was deleted, is that parent's 404, and nothing is written.
    [Fact]
    public async Task Create_BoardThatDoesNotExistOrWasDeleted_IsOrganizerNotFound()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var deleted = await BoardAsync(admin);
        (await admin.DeleteAsync($"{Organizers}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var board in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board), AppJson.Options);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNotFound);
        }

        (await ListAsync(admin, exam.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ExamThatDoesNotExistOrWasDeleted_IsExamNotFound()
    {
        var admin = await AdminAsync();
        var board = await BoardAsync(admin);
        var deleted = await ExamAsync(admin);
        (await admin.DeleteAsync($"{Exams}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var exam in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await admin.PostAsJsonAsync(EditionsOf(exam), Valid(board.Id), AppJson.Options);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNotFound);
        }
    }

    [Fact]
    public async Task AnEditionIdUnderAnotherExamsRoute_IsEditionNotFound()
    {
        var admin = await AdminAsync();
        var board = await BoardAsync(admin);
        var owner = await ExamAsync(admin);
        var stranger = await ExamAsync(admin);
        var edition = await CreateAsync(admin, owner.Id, Valid(board.Id));

        var get = await admin.GetAsync($"{EditionsOf(stranger.Id)}/{edition.Id}");
        var put = await admin.PutAsJsonAsync($"{EditionsOf(stranger.Id)}/{edition.Id}", Valid(board.Id), AppJson.Options);
        var delete = await admin.DeleteAsync($"{EditionsOf(stranger.Id)}/{edition.Id}");

        foreach (var response in new[] { get, put, delete })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNotFound);
        }
    }

    // AC11: publishing and unpublishing both work through the same save.
    [Fact]
    public async Task Update_PublishesAndUnpublishesAnEdition()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam.Id, Valid(board.Id));

        var published = await admin.PutAsJsonAsync($"{EditionsOf(exam.Id)}/{created.Id}", Valid(board.Id, status: "Published"), AppJson.Options);
        (await published.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!.Status
            .Should().Be(ExamEditionStatus.Published);

        var draft = await admin.PutAsJsonAsync($"{EditionsOf(exam.Id)}/{created.Id}", Valid(board.Id, status: "Draft"), AppJson.Options);
        (await draft.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!.Status
            .Should().Be(ExamEditionStatus.Draft);
    }

    // AC15 and BR9: an unknown status is the field's own 400. A blank one means Draft only when adding.
    [Theory]
    [InlineData("InReview")]
    [InlineData("Archived")]
    [InlineData("2")]
    public async Task Create_StatusThatIsNotDraftOrPublished_IsRefusedAndNothingIsWritten(string status)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, status: status), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionStatusInvalid);
        (await ListAsync(admin, exam.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Update_WithoutAStatus_IsRefusedSoAPublishedEditionIsNeverSilentlyUnpublished()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam.Id, Valid(board.Id, status: "Published"));

        var response = await admin.PutAsJsonAsync($"{EditionsOf(exam.Id)}/{created.Id}", Valid(board.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionStatusInvalid);
        (await ListAsync(admin, exam.Id)).Single().Status.Should().Be(ExamEditionStatus.Published);
    }

    [Fact]
    public async Task OpenApiDocument_NamesTheTwoStatusValues()
    {
        var document = await Client().GetStringAsync("/openapi/v1.json");

        using var parsed = JsonDocument.Parse(document);
        var description = parsed.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SaveExamEditionRequest").GetProperty("properties")
            .GetProperty("status").GetProperty("description").GetString();
        description.Should().Contain("Draft").And.Contain("Published");
    }

    // AC12: a draft leaves after the confirmation; the row stays, flagged, and its key stays taken.
    [Fact]
    public async Task Delete_DraftEdition_HidesItKeepsTheRowAndKeepsItsKeyTaken()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam.Id, Valid(board.Id, position: "Guarda"));

        (await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(admin, exam.Id)).Should().BeEmpty();
        (await admin.GetAsync($"{EditionsOf(exam.Id)}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var row = await QueryAsync(context => context.ExamEditions
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(item => item.Id == created.Id));
        row.IsDeleted.Should().BeTrue();

        var again = await admin.PostAsJsonAsync(EditionsOf(exam.Id), Valid(board.Id, position: "GUARDA"), AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionDuplicate);
    }

    // AC12 and BR11: a published edition is refused with its own code and stays until it is a draft again.
    [Fact]
    public async Task Delete_PublishedEdition_IsRefusedAndItStaysUntilItIsSetBackToDraft()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam.Id, Valid(board.Id, status: "Published"));

        var refused = await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{created.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionPublished);
        (await ListAsync(admin, exam.Id)).Should().ContainSingle(item => item.Id == created.Id);

        (await admin.PutAsJsonAsync($"{EditionsOf(exam.Id)}/{created.Id}", Valid(board.Id, status: "Draft"), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AnIdThatIsNotAnEdition_IsEditionNotFound()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);

        var response = await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionNotFound);
    }

    // AC13: an exam that has editions does not leave; once they are deleted it can.
    [Fact]
    public async Task DeleteExam_ThatHasEditions_IsRefusedAndItStaysUntilTheEditionsAreDeleted()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var edition = await CreateAsync(admin, exam.Id, Valid(board.Id));

        var refused = await admin.DeleteAsync($"{Exams}/{exam.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamHasEditions);
        (await admin.GetAsync($"{Exams}/{exam.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{edition.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Exams}/{exam.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // AC14: a board that an edition names does not leave; once that edition is deleted it can.
    [Fact]
    public async Task DeleteOrganizer_ThatAnEditionNames_IsRefusedAndItStaysUntilTheEditionIsDeleted()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var edition = await CreateAsync(admin, exam.Id, Valid(board.Id));

        var refused = await admin.DeleteAsync($"{Organizers}/{board.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerHasEditions);
        var listed = await admin.GetFromJsonAsync<OrganizerPageResponse>(
            $"{Organizers}?search={Uri.EscapeDataString(board.Acronym)}",
            AppJson.Options);
        listed!.Items.Should().ContainSingle(item => item.Id == board.Id);

        (await admin.DeleteAsync($"{EditionsOf(exam.Id)}/{edition.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"{Organizers}/{board.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
