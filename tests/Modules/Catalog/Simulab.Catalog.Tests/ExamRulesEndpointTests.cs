using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-168 through HTTP, as the Web calls it: the exam rules an admin records on an edition, the points per question
/// of its notice subjects, the objective total, and where the student and the other modules read them. The tests of
/// this class share one database, so each one works on its own exam and never asserts a global count.
/// </summary>
public sealed class ExamRulesEndpointTests : CatalogApiTests
{
    private const string Exams = "/api/v1/catalog/exams";
    private const string Organizers = "/api/v1/catalog/organizers";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";
    private const string Published = "/api/v1/catalog/published-exams";
    private const int NoticeYear = 2024;

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static string EditionsOf(Guid exam) => $"{Exams}/{exam}/editions";

    private static string SubjectsOf(Guid exam, Guid edition) => $"{EditionsOf(exam)}/{edition}/notice-subjects";

    private static SaveExamEditionRequest Rules(
        Guid board,
        string status = "Draft",
        int? duration = null,
        decimal? objectiveCutOff = null,
        decimal? essayMax = null,
        decimal? essayCutOff = null) =>
        new(board, NoticeYear, null, null, null, null, status, duration, objectiveCutOff, essayMax, essayCutOff);

    private static async Task<Guid> ExamAsync(HttpClient admin)
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

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!.Id;
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

    private static async Task<ExamEditionResponse> CreateAsync(HttpClient admin, Guid exam, SaveExamEditionRequest request)
    {
        var response = await admin.PostAsJsonAsync(EditionsOf(exam), request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    private static Task<HttpResponseMessage> UpdateAsync(HttpClient admin, Guid exam, Guid edition, SaveExamEditionRequest request) =>
        admin.PutAsJsonAsync($"{EditionsOf(exam)}/{edition}", request, AppJson.Options);

    private static async Task<ExamEditionResponse> FindAsync(HttpClient admin, Guid exam, Guid edition) =>
        (await admin.GetFromJsonAsync<ExamEditionResponse>($"{EditionsOf(exam)}/{edition}", AppJson.Options))!;

    private static Task<HttpResponseMessage> AddSubjectAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        int? count,
        decimal? points) =>
        admin.PostAsJsonAsync(
            SubjectsOf(exam, edition),
            new SaveNoticeSubjectRequest(null, label, count, null, points),
            AppJson.Options);

    private static async Task<NoticeSubjectResponse> CreateSubjectAsync(
        HttpClient admin,
        Guid exam,
        Guid edition,
        string label,
        int? count,
        decimal? points)
    {
        var response = await AddSubjectAsync(admin, exam, edition, label, count, points);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<NoticeSubjectResponse>(AppJson.Options))!;
    }

    private async Task<T> DirectoryAsync<T>(Func<IExamEditionDirectory, Task<T>> read)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        return await read(scope.ServiceProvider.GetRequiredService<IExamEditionDirectory>());
    }

    // AC1: the four rules come back from the save and from a later read; 5 h 00 min travels as 300 minutes.
    [Fact]
    public async Task Save_WithEveryRule_ReturnsAndKeepsThem()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var created = await CreateAsync(admin, exam, Rules(board, duration: 300, objectiveCutOff: 60m, essayMax: 20m, essayCutOff: 10m));

        created.DurationMinutes.Should().Be(300);
        var read = await FindAsync(admin, exam, created.Id);
        read.DurationMinutes.Should().Be(300);
        read.ObjectiveCutOffScore.Should().Be(60.00m);
        read.EssayMaxScore.Should().Be(20.00m);
        read.EssayCutOffScore.Should().Be(10.00m);
    }

    // AC2 and BR1: a save that leaves every rule empty stores none; a PUT replaces every field.
    [Fact]
    public async Task Update_WithEveryRuleCleared_SavesTheEditionWithNoRule()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam, Rules(board, duration: 300, objectiveCutOff: 60m, essayMax: 20m, essayCutOff: 10m));
        created.EssayCutOffScore.Should().Be(10.00m, "the edition starts with every rule");

        var response = await UpdateAsync(admin, exam, created.Id, Rules(board));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var read = await FindAsync(admin, exam, created.Id);
        read.DurationMinutes.Should().BeNull();
        read.ObjectiveCutOffScore.Should().BeNull();
        read.EssayMaxScore.Should().BeNull();
        read.EssayCutOffScore.Should().BeNull();
    }

    // AC3 and BR2: 15 to 480 minutes, both ends included.
    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    [InlineData(0)]
    public async Task Save_DurationOutOfRange_IsRefusedWithItsCode(int minutes)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(EditionsOf(exam), Rules(board, duration: minutes), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionDurationInvalid);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(480)]
    public async Task Save_DurationAtEitherEnd_IsAccepted(int minutes)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var created = await CreateAsync(admin, exam, Rules(board, duration: minutes));

        created.DurationMinutes.Should().Be(minutes);
    }

    // AC6 and BR5: the objective cut-off is 0.01 to 1000.00 with at most 2 decimal places.
    [Theory]
    [InlineData("0")]
    [InlineData("1000.01")]
    [InlineData("60.005")]
    public async Task Save_ObjectiveCutOffOutOfRange_IsRefusedWithItsCode(string cutOff)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(
            EditionsOf(exam),
            Rules(board, objectiveCutOff: decimal.Parse(cutOff, System.Globalization.CultureInfo.InvariantCulture)),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamEditionObjectiveCutOffInvalid);
    }

    // AC6 and BR5: a cut-off above a known objective total is a warning on the form, never a refusal.
    [Fact]
    public async Task Save_ObjectiveCutOffAboveTheKnownTotal_IsSaved()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam, Rules(board));
        await CreateSubjectAsync(admin, exam, created.Id, "Língua Portuguesa", 10, 1.00m);

        var response = await UpdateAsync(admin, exam, created.Id, Rules(board, objectiveCutOff: 60m));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var read = await FindAsync(admin, exam, created.Id);
        read.ObjectiveMaxScore.Should().Be(10.00m);
        read.ObjectiveCutOffScore.Should().Be(60.00m);
    }

    // AC7 and BR6: each essay breach has its own code, the range of each value before the relation between them.
    [Theory]
    [InlineData(null, "10", CatalogErrorCodes.ExamEditionEssayCutOffWithoutMax)]
    [InlineData("20", "20.01", CatalogErrorCodes.ExamEditionEssayCutOffAboveMax)]
    [InlineData("0", null, CatalogErrorCodes.ExamEditionEssayMaxScoreInvalid)]
    [InlineData("1000.01", "10", CatalogErrorCodes.ExamEditionEssayMaxScoreInvalid)]
    [InlineData("20", "0", CatalogErrorCodes.ExamEditionEssayCutOffInvalid)]
    [InlineData("20", "10.001", CatalogErrorCodes.ExamEditionEssayCutOffInvalid)]
    [InlineData(null, "1000.01", CatalogErrorCodes.ExamEditionEssayCutOffInvalid)]
    public async Task Save_EssayBreach_IsRefusedWithItsCode(string? essayMax, string? essayCutOff, string code)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var response = await admin.PostAsJsonAsync(
            EditionsOf(exam),
            Rules(board, essayMax: Parse(essayMax), essayCutOff: Parse(essayCutOff)),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
    }

    [Fact]
    public async Task Save_EssayCutOffEqualToTheMaximum_IsAccepted()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);

        var created = await CreateAsync(admin, exam, Rules(board, essayMax: 20m, essayCutOff: 20m));

        created.EssayCutOffScore.Should().Be(20.00m);
    }

    // AC8 and BR1: a published edition with no rule saves and stays published.
    [Fact]
    public async Task Save_PublishedEditionWithNoRule_StaysPublished()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var created = await CreateAsync(admin, exam, Rules(board, status: "Published", duration: 300));
        created.DurationMinutes.Should().Be(300, "the edition starts with a rule");

        var response = await UpdateAsync(admin, exam, created.Id, Rules(board, status: "Published"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var read = await FindAsync(admin, exam, created.Id);
        read.Status.Should().Be(ExamEditionStatus.Published);
        read.DurationMinutes.Should().BeNull();
    }

    // AC4 and BR3: points per question are 0.01 to 100.00 with at most 2 decimal places.
    [Theory]
    [InlineData("0")]
    [InlineData("100.01")]
    [InlineData("1.555")]
    [InlineData("-1")]
    public async Task SaveNoticeSubject_PointsOutOfRange_IsRefusedWithItsCode(string points)
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var edition = await CreateAsync(admin, exam, Rules(await BoardAsync(admin)));

        var response = await AddSubjectAsync(
            admin, exam, edition.Id, "Informática", 5, decimal.Parse(points, System.Globalization.CultureInfo.InvariantCulture));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.NoticeSubjectPointsPerQuestionInvalid);
    }

    [Fact]
    public async Task SaveNoticeSubject_PointsAtEitherEnd_AreKeptAndAnUpdateWithoutThemClearsThem()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var edition = await CreateAsync(admin, exam, Rules(await BoardAsync(admin)));

        var cheap = await CreateSubjectAsync(admin, exam, edition.Id, "Informática", 5, 0.01m);
        var dear = await CreateSubjectAsync(admin, exam, edition.Id, "Direito Penal", 5, 100.00m);
        var cleared = await admin.PutAsJsonAsync(
            $"{SubjectsOf(exam, edition.Id)}/{dear.Id}",
            new SaveNoticeSubjectRequest(null, "Direito Penal", 5),
            AppJson.Options);

        cheap.PointsPerQuestion.Should().Be(0.01m);
        dear.PointsPerQuestion.Should().Be(100.00m);
        cleared.StatusCode.Should().Be(HttpStatusCode.OK, await cleared.Content.ReadAsStringAsync());
        var rows = (await admin.GetFromJsonAsync<List<NoticeSubjectResponse>>(SubjectsOf(exam, edition.Id), AppJson.Options))!;
        rows.Single(row => row.Id == dear.Id).PointsPerQuestion.Should().BeNull();
        rows.Single(row => row.Id == cheap.Id).PointsPerQuestion.Should().Be(0.01m);
    }

    // AC5 and BR4: the objective total is computed from the live subjects, and unknown while one lacks a value.
    [Fact]
    public async Task Find_ObjectiveMaxScore_IsKnownOnlyWhenEveryLiveSubjectHasCountAndPoints()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var edition = await CreateAsync(admin, exam, Rules(await BoardAsync(admin)));
        (await FindAsync(admin, exam, edition.Id)).ObjectiveMaxScore.Should().BeNull("an edition with no subject has no total");

        await CreateSubjectAsync(admin, exam, edition.Id, "Língua Portuguesa", 10, 1.50m);
        var penal = await CreateSubjectAsync(admin, exam, edition.Id, "Direito Penal", 5, null);
        (await FindAsync(admin, exam, edition.Id)).ObjectiveMaxScore.Should().BeNull("a subject has no points");

        var deleted = await admin.DeleteAsync($"{SubjectsOf(exam, edition.Id)}/{penal.Id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await CreateSubjectAsync(admin, exam, edition.Id, "Legislação específica", 15, 2.00m);

        (await FindAsync(admin, exam, edition.Id)).ObjectiveMaxScore.Should().Be(45.00m, "a deleted subject is not live");
        var listed = (await admin.GetFromJsonAsync<List<ExamEditionResponse>>(EditionsOf(exam), AppJson.Options))!;
        listed.Single(row => row.Id == edition.Id).ObjectiveMaxScore.Should().Be(45.00m);
    }

    // AC9 and BR7: a copy of the notice subjects carries their points.
    [Fact]
    public async Task CopyNoticeSubjects_CarriesThePointsPerQuestion()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var source = await CreateAsync(admin, exam, Rules(board));
        var target = await CreateAsync(admin, exam, Rules(board) with { Position = "Outro cargo" });
        await CreateSubjectAsync(admin, exam, source.Id, "Língua Portuguesa", 10, 1.50m);
        await CreateSubjectAsync(admin, exam, source.Id, "Direito Constitucional", 5, 2.00m);
        await CreateSubjectAsync(admin, exam, source.Id, "Informática", 5, null);

        var copied = await admin.PostAsJsonAsync(
            $"{SubjectsOf(exam, target.Id)}/copy", new CopyNoticeSubjectsRequest(source.Id), AppJson.Options);

        copied.IsSuccessStatusCode.Should().BeTrue(await copied.Content.ReadAsStringAsync());
        var rows = (await admin.GetFromJsonAsync<List<NoticeSubjectResponse>>(SubjectsOf(exam, target.Id), AppJson.Options))!;
        rows.Select(row => (row.Label, row.PointsPerQuestion)).Should().Equal(
            ("Língua Portuguesa", 1.50m), ("Direito Constitucional", 2.00m), ("Informática", (decimal?)null));
    }

    // AC10: the published read carries the rules and the objective total of each published edition.
    [Fact]
    public async Task PublishedRead_CarriesTheRulesAndTheObjectiveTotal()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        var withRules = await CreateAsync(
            admin, exam, Rules(board, status: "Published", duration: 300, objectiveCutOff: 60m, essayMax: 20m, essayCutOff: 10m));
        await CreateAsync(admin, exam, Rules(board, status: "Published") with { NoticeYear = 2023 });
        await CreateSubjectAsync(admin, exam, withRules.Id, "Língua Portuguesa", 10, 10.00m);

        var detail = (await student.GetFromJsonAsync<PublishedExamDetailResponse>($"{Published}/{exam}", AppJson.Options))!;

        var rules = detail.Editions.Single(edition => edition.Id == withRules.Id);
        rules.DurationMinutes.Should().Be(300);
        rules.ObjectiveMaxScore.Should().Be(100.00m);
        rules.ObjectiveCutOffScore.Should().Be(60.00m);
        rules.EssayMaxScore.Should().Be(20.00m);
        rules.EssayCutOffScore.Should().Be(10.00m);
        var none = detail.Editions.Single(edition => edition.NoticeYear == 2023);
        none.DurationMinutes.Should().BeNull();
        none.ObjectiveMaxScore.Should().BeNull();
        none.ObjectiveCutOffScore.Should().BeNull();
        none.EssayMaxScore.Should().BeNull();
        none.EssayCutOffScore.Should().BeNull();
    }

    // AC11 and BR8: the rules change only through catalog.manage, and a draft's rules never reach the published read.
    [Fact]
    public async Task SaveRoutes_WithoutTheManagePermission_AreForbidden_AndADraftsRulesAreNeverPublished()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var exam = await ExamAsync(admin);
        var board = await BoardAsync(admin);
        await CreateAsync(admin, exam, Rules(board, status: "Published"));
        var draft = await CreateAsync(admin, exam, Rules(board, duration: 300, objectiveCutOff: 60m) with { NoticeYear = 2023 });
        var rules = Rules(board, duration: 300, objectiveCutOff: 60m, essayMax: 20m, essayCutOff: 10m);

        var responses = new[]
        {
            await student.PostAsJsonAsync(EditionsOf(exam), rules, AppJson.Options),
            await student.PutAsJsonAsync($"{EditionsOf(exam)}/{draft.Id}", rules, AppJson.Options),
            await student.PostAsJsonAsync(SubjectsOf(exam, draft.Id), new SaveNoticeSubjectRequest(null, "Informática", 5, null, 1.50m), AppJson.Options),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }

        var detail = (await student.GetFromJsonAsync<PublishedExamDetailResponse>($"{Published}/{exam}", AppJson.Options))!;
        detail.Editions.Select(edition => edition.Id).Should().NotContain(draft.Id);
        (await FindAsync(admin, exam, draft.Id)).DurationMinutes.Should().Be(300, "the student's PUT changed nothing");
    }

    // AC12 and UC5: another module reads the rules, the objective total and the points through the directory.
    [Fact]
    public async Task Directory_CarriesTheRulesTheObjectiveTotalAndThePoints()
    {
        var admin = await AdminAsync();
        var exam = await ExamAsync(admin);
        var edition = await CreateAsync(
            admin, exam, Rules(await BoardAsync(admin), duration: 300, objectiveCutOff: 60m, essayMax: 20m, essayCutOff: 10m));
        await CreateSubjectAsync(admin, exam, edition.Id, "Língua Portuguesa", 10, 1.50m);
        await CreateSubjectAsync(admin, exam, edition.Id, "Legislação específica", 15, 2.00m);

        var entry = await DirectoryAsync(directory => directory.FindAsync(edition.Id, CancellationToken.None));
        var many = await DirectoryAsync(directory => directory.FindManyAsync([edition.Id], CancellationToken.None));
        var subjects = await DirectoryAsync(directory => directory.ListNoticeSubjectsAsync(edition.Id, CancellationToken.None));

        entry!.DurationMinutes.Should().Be(300);
        entry.ObjectiveMaxScore.Should().Be(45.00m);
        entry.ObjectiveCutOffScore.Should().Be(60.00m);
        entry.EssayMaxScore.Should().Be(20.00m);
        entry.EssayCutOffScore.Should().Be(10.00m);
        many.Single().ObjectiveMaxScore.Should().Be(45.00m);
        subjects.Select(row => (row.Label, row.PointsPerQuestion)).Should().Equal(
            ("Língua Portuguesa", (decimal?)1.50m), ("Legislação específica", (decimal?)2.00m));
    }

    private static decimal? Parse(string? value) =>
        value is null ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
