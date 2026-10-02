using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-36 through HTTP and through <see cref="IPublishedExamQueries"/>. The tests of this class share one
/// database, so each one works on its own exams, boards and search token and never asserts a global count.
/// </summary>
public sealed class PublishedExamEndpointTests : CatalogApiTests
{
    private const string Exams = "/api/v1/catalog/exams";
    private const string Organizers = "/api/v1/catalog/organizers";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";
    private const string Published = "/api/v1/catalog/published-exams";
    private const string Filters = "/api/v1/catalog/published-exam-filters";

    private static string Token() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<IssuingAuthorityResponse> AuthorityAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(
            Authorities,
            new SaveIssuingAuthorityRequest(name),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!;
    }

    private static async Task<ExamResponse> ExamAsync(
        HttpClient admin,
        Guid authority,
        string name,
        AssessmentType type = AssessmentType.PublicServiceExam,
        ExamScope scope = ExamScope.National,
        string? detail = null)
    {
        var response = await admin.PostAsJsonAsync(
            Exams,
            new SaveExamRequest(authority, name, type.ToString(), scope.ToString(), detail, "pt-BR"),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
    }

    private static async Task<OrganizerResponse> BoardAsync(HttpClient admin, string? name = null, string? acronym = null)
    {
        var response = await admin.PostAsJsonAsync(
            Organizers,
            new SaveOrganizerRequest(name ?? $"Banca {Token()}", acronym ?? Token(), nameof(OrganizerKind.ExamBoard)),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
    }

    private static async Task<ExamEditionResponse> EditionAsync(
        HttpClient admin,
        Guid exam,
        Guid board,
        int year,
        string status,
        string? position = null,
        string? reference = null,
        string? url = null,
        DateOnly? appliedOn = null)
    {
        var response = await admin.PostAsJsonAsync(
            $"{Exams}/{exam}/editions",
            new SaveExamEditionRequest(board, year, position, reference, url, appliedOn, status),
            AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamEditionResponse>(AppJson.Options))!;
    }

    private static Task<PublishedExamPageResponse> ListAsync(HttpClient client, string query) =>
        client.GetFromJsonAsync<PublishedExamPageResponse>($"{Published}?{query}", AppJson.Options)!;

    private static string Search(string token, string more = "") => $"search={Uri.EscapeDataString(token)}{more}";

    // AC1, AC13: a draft edition and a draft-only exam never reach the student side, whoever asks.
    [Fact]
    public async Task List_ExamWithDraftsOnly_IsNotListed_AndAPublishedOneCountsOnlyItsPublishedEditions()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        var withPublished = await ExamAsync(admin, authority.Id, $"Exame A {token}");
        var draftsOnly = await ExamAsync(admin, authority.Id, $"Exame B {token}");
        await EditionAsync(admin, withPublished.Id, board.Id, 2024, "Published");
        await EditionAsync(admin, withPublished.Id, board.Id, 2025, "Draft");
        await EditionAsync(admin, draftsOnly.Id, board.Id, 2025, "Draft");

        var forStudent = await ListAsync(student, Search(token));
        var forAdmin = await ListAsync(admin, Search(token));

        foreach (var page in new[] { forStudent, forAdmin })
        {
            page.Total.Should().Be(1);
            var row = page.Items.Should().ContainSingle().Subject;
            row.Id.Should().Be(withPublished.Id);
            row.PublishedEditionCount.Should().Be(1);
            row.LatestNoticeYear.Should().Be(2024);
            row.IssuingAuthorityName.Should().Be(authority.Name);
        }
    }

    // AC2: ordered by name, paged, and Total counts every match.
    [Fact]
    public async Task List_ThreeExams_AreOrderedByNameAndPaged()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        foreach (var name in new[] { "Charlie", "Alpha", "Bravo" })
        {
            var exam = await ExamAsync(admin, authority.Id, $"{name} {token}");
            await EditionAsync(admin, exam.Id, board.Id, 2025, "Published");
        }

        var first = await ListAsync(student, Search(token, "&page=0&pageSize=2"));
        var second = await ListAsync(student, Search(token, "&page=1&pageSize=2"));

        first.Total.Should().Be(3);
        first.Items.Select(item => item.Name).Should().Equal($"Alpha {token}", $"Bravo {token}");
        second.Total.Should().Be(3);
        second.Items.Select(item => item.Name).Should().Equal($"Charlie {token}");
    }

    // AC3: word by word, ignoring case and accents, across exam, authority and scope detail; a stored
    // acronym is not searched (F-44 AC7).
    [Fact]
    public async Task List_SearchWords_MatchAcrossExamAuthorityAndScopeDetail()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var acronym = $"PM{Token()[..8]}";
        var authority = await AuthorityAsync(admin, $"Prefeitura de São Paulo {token}");
        await StoreAcronymAsync(authority.Id, acronym);
        var board = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Guarda Municipal {token}", scope: ExamScope.Municipal, detail: "São Paulo");
        await EditionAsync(admin, exam.Id, board.Id, 2025, "Published");

        (await ListAsync(student, Search($"guarda sao {token}"))).Items.Should().ContainSingle().Which.Id.Should().Be(exam.Id);
        (await ListAsync(student, Search(acronym.ToLowerInvariant()))).Items.Should().BeEmpty("the stored acronym is no longer searched (F-44 BR5)");
        (await ListAsync(student, Search($"GUARDA  paulo   {token}"))).Items.Should().ContainSingle().Which.Id.Should().Be(exam.Id);
        (await ListAsync(student, Search($"guarda rio {token}"))).Items.Should().BeEmpty();
    }

    // AC5: the board and the year match on the same published edition; type and scope narrow too.
    [Fact]
    public async Task List_BoardAndYear_MatchOnTheSameEdition()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var fgv = await BoardAsync(admin);
        var cebraspe = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Exame {token}", scope: ExamScope.State, detail: "GO");
        await EditionAsync(admin, exam.Id, fgv.Id, 2024, "Published");
        await EditionAsync(admin, exam.Id, cebraspe.Id, 2025, "Published");

        (await ListAsync(student, Search(token, $"&organizerId={fgv.Id}&noticeYear=2025"))).Items.Should().BeEmpty();
        var match = await ListAsync(student, Search(token, $"&organizerId={fgv.Id}&noticeYear=2024"));
        match.Items.Should().ContainSingle();
        match.Items[0].PublishedEditionCount.Should().Be(2, "the row says what the exam has, not why it matched");
        (await ListAsync(student, Search(token, $"&organizerId={cebraspe.Id}&noticeYear=2025"))).Items.Should().ContainSingle();
        (await ListAsync(student, Search(token, "&assessmentType=Certification"))).Items.Should().BeEmpty();
        (await ListAsync(student, Search(token, "&scope=National"))).Items.Should().BeEmpty();
        (await ListAsync(student, Search(token, "&assessmentType=PublicServiceExam&scope=State"))).Items.Should().ContainSingle();
    }

    // AC6: a value the Api cannot read is no filter, not an error.
    [Fact]
    public async Task List_UnreadableFilters_AreIgnored()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Exame {token}");
        await EditionAsync(admin, exam.Id, board.Id, 2025, "Published");

        var response = await student.GetAsync(
            $"{Published}?{Search(token)}&assessmentType=Foo&scope=Bar&organizerId={Guid.CreateVersion7()}x&noticeYear=abc");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var page = (await response.Content.ReadFromJsonAsync<PublishedExamPageResponse>(AppJson.Options))!;
        page.Items.Should().ContainSingle().Which.Id.Should().Be(exam.Id);
    }

    // Review of F-36: words split on any whitespace (a pasted tab), and an enum filter is read by name only.
    [Fact]
    public async Task List_TabBetweenWords_AndNumericEnumFilters_BehaveAsTheBookmarkPageDoes()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Guarda Municipal {token}", scope: ExamScope.State, detail: "GO");
        await EditionAsync(admin, exam.Id, board.Id, 2025, "Published");

        (await ListAsync(student, $"search={Uri.EscapeDataString($"guarda\tgoias\n{token}")}")).Items
            .Should().ContainSingle().Which.Id.Should().Be(exam.Id);
        // 1 is National: read as a number it would hide this State exam.
        (await ListAsync(student, Search(token, "&scope=1"))).Items.Should().ContainSingle();
    }

    // F-42 AC6, AC7 (BR4, BR5): a published State exam stored as SP comes back with its acronym and is found by
    // the acronym, the name and the name with its accent, in any case; a student who searches another state
    // does not find it.
    [Theory]
    [InlineData("sp")]
    [InlineData("SP")]
    [InlineData("sao paulo")]
    [InlineData("São Paulo")]
    [InlineData("paulo")]
    public async Task List_StateExamStoredAsAnAcronym_IsFoundByTheStateNameOrAcronym(string term)
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Exame {token}", scope: ExamScope.State, detail: "sp");
        await EditionAsync(admin, exam.Id, board.Id, 2025, "Published");

        var found = await ListAsync(student, $"search={Uri.EscapeDataString($"{term} {token}")}");

        found.Items.Should().ContainSingle().Which.ScopeDetail.Should().Be("SP");
        (await ListAsync(student, $"search={Uri.EscapeDataString($"ceara {token}")}")).Items.Should().BeEmpty();
    }

    // AC7: only boards and years that name a published edition are offered.
    [Fact]
    public async Task Filters_OfferOnlyWhatHasAPublishedEdition()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var publishedBoard = await BoardAsync(admin, acronym: $"B{Token()[..8]}");
        var draftBoard = await BoardAsync(admin, acronym: $"A{Token()[..8]}");
        var exam = await ExamAsync(admin, authority.Id, $"Exame {token}");
        await EditionAsync(admin, exam.Id, publishedBoard.Id, 2011, "Published");
        await EditionAsync(admin, exam.Id, draftBoard.Id, 2012, "Draft");

        var filters = (await student.GetFromJsonAsync<PublishedExamFiltersResponse>(Filters, AppJson.Options))!;

        filters.Organizers.Should().Contain(organizer => organizer.Id == publishedBoard.Id);
        filters.Organizers.Should().NotContain(organizer => organizer.Id == draftBoard.Id);
        filters.Organizers.Select(organizer => organizer.Acronym).Should().BeInAscendingOrder(StringComparer.Ordinal);
        filters.NoticeYears.Should().Contain(2011);
        filters.NoticeYears.Should().NotContain(2012);
        filters.NoticeYears.Should().BeInDescendingOrder();
    }

    // AC8, AC10 (data side): every published edition, newest first, with what the screen shows.
    [Fact]
    public async Task Find_ShowsThePublishedEditionsNewestFirst_AndNoDraft()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var board = await BoardAsync(admin);
        var exam = await ExamAsync(admin, authority.Id, $"Exame {token}");
        await EditionAsync(admin, exam.Id, board.Id, 2023, "Published", "Analista");
        await EditionAsync(admin, exam.Id, board.Id, 2025, "Published", "Técnico", "Edital 01/2025", "https://exemplo.com/edital", new DateOnly(2025, 5, 4));
        await EditionAsync(admin, exam.Id, board.Id, 2024, "Draft", "Rascunho");

        var response = await student.GetAsync($"{Published}/{exam.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var detail = (await response.Content.ReadFromJsonAsync<PublishedExamDetailResponse>(AppJson.Options))!;
        detail.Name.Should().Be(exam.Name);
        detail.PublishedEditionCount.Should().Be(2);
        detail.LatestNoticeYear.Should().Be(2025);
        detail.Editions.Select(edition => edition.NoticeYear).Should().Equal(2025, 2023);
        var latest = detail.Editions[0];
        latest.Position.Should().Be("Técnico");
        latest.OrganizerName.Should().Be(board.Name);
        latest.OrganizerAcronym.Should().Be(board.Acronym);
        latest.NoticeReference.Should().Be("Edital 01/2025");
        latest.NoticeUrl.Should().Be("https://exemplo.com/edital");
        latest.AppliedOn.Should().Be(new DateOnly(2025, 5, 4));
    }

    // AC9: a draft-only exam, a deleted exam and a random id are all the same 404.
    [Fact]
    public async Task Find_DraftOnlyDeletedOrUnknownExam_IsNotFound()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var authority = await AuthorityAsync(admin, $"Orgao {Token()}");
        var board = await BoardAsync(admin);
        var draftOnly = await ExamAsync(admin, authority.Id, $"Rascunho {Token()}");
        await EditionAsync(admin, draftOnly.Id, board.Id, 2025, "Draft");
        var deleted = await ExamAsync(admin, authority.Id, $"Removido {Token()}");
        (await admin.DeleteAsync($"{Exams}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var id in new[] { draftOnly.Id, deleted.Id, Guid.CreateVersion7() })
        {
            var response = await student.GetAsync($"{Published}/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync()).Should().Contain("exam.not_found");
        }
    }

    // AC12: catalog.manage alone opens nothing on the student side; catalog.browse does.
    [Fact]
    public async Task EveryRoute_WithoutBrowse_IsForbidden_EvenWithManage()
    {
        var admin = await AdminAsync();
        var roleName = $"Curador {Token()}";
        (await admin.PostAsJsonAsync("/api/v1/identity/roles", new SaveRoleRequest(roleName, [CatalogPermissions.Manage]), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await TestAccounts.CreateAsync(Factory.Services, roles: roleName);
        var manager = await TestAccounts.SignedInAsync(Client(), account.Email!);

        foreach (var route in new[] { Published, $"{Published}/{Guid.CreateVersion7()}", Filters })
        {
            var response = await manager.GetAsync(route);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, route);
            (await response.Content.ReadAsStringAsync()).Should().Contain("identity.forbidden");
        }
    }

    // AC14: the interface other modules call answers as the endpoints do.
    [Fact]
    public async Task PublishedExamQueries_ResolvedFromTheContainer_AnswersLikeTheEndpoints()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var fgv = await BoardAsync(admin);
        var other = await BoardAsync(admin);
        var listed = await ExamAsync(admin, authority.Id, $"Listado {token}");
        var hidden = await ExamAsync(admin, authority.Id, $"Oculto {token}");
        await EditionAsync(admin, listed.Id, fgv.Id, 2024, "Published");
        await EditionAsync(admin, listed.Id, other.Id, 2025, "Published");
        await EditionAsync(admin, hidden.Id, fgv.Id, 2025, "Draft");

        await using var scope = Factory.Services.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPublishedExamQueries>();

        var list = await queries.ListAsync(new PublishedExamListQuery(Search: token), CancellationToken.None);
        list.Should().BeEquivalentTo(await ListAsync(student, Search(token)));
        list.Items.Should().ContainSingle().Which.Id.Should().Be(listed.Id);

        var sameEdition = await queries.ListAsync(new PublishedExamListQuery(Search: token, OrganizerId: fgv.Id, NoticeYear: 2025), CancellationToken.None);
        sameEdition.Items.Should().BeEmpty();

        var found = await queries.FindAsync(listed.Id, CancellationToken.None);
        found.Should().BeEquivalentTo(await student.GetFromJsonAsync<PublishedExamDetailResponse>($"{Published}/{listed.Id}", AppJson.Options));
        found!.Editions.Select(edition => edition.NoticeYear).Should().Equal(2025, 2024);
        (await queries.FindAsync(hidden.Id, CancellationToken.None)).Should().BeNull();

        var oversized = await queries.ListAsync(new PublishedExamListQuery(Page: -3, PageSize: 10_000, Search: token), CancellationToken.None);
        oversized.Total.Should().Be(1, "the cap and the page floor belong to the contract, not to the endpoint");
    }
}
