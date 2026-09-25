using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-34 through HTTP, as the Web calls it. The tests of this class share one database, so each one works
/// on its own issuing authority and never asserts a global count.
/// </summary>
public sealed class ExamEndpointTests : CatalogApiTests
{
    private const string Exams = "/api/v1/catalog/exams";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static SaveExamRequest Valid(
        Guid authority,
        string? name = null,
        AssessmentType assessmentType = AssessmentType.PublicServiceExam,
        ExamScope scope = ExamScope.National,
        string? scopeDetail = null,
        string? contentLanguage = "pt-BR") =>
        new(authority, name ?? Unique("Exame"), assessmentType.ToString(), scope.ToString(), scopeDetail, contentLanguage);

    /// <summary>A fresh issuing authority, so each test owns the namespace its exam names live in (BR10).</summary>
    private static async Task<IssuingAuthorityResponse> AuthorityAsync(HttpClient admin)
    {
        var request = new SaveIssuingAuthorityRequest(Unique("Orgao"), Guid.CreateVersion7().ToString("N")[..12]);

        var response = await admin.PostAsJsonAsync(Authorities, request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!;
    }

    private static async Task<ExamResponse> CreateAsync(HttpClient admin, SaveExamRequest request)
    {
        var response = await admin.PostAsJsonAsync(Exams, request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
    }

    private static async Task<ExamPageResponse> ListAsync(HttpClient admin, string query = "") =>
        (await admin.GetFromJsonAsync<ExamPageResponse>(Exams + query, AppJson.Options))!;

    // AC1: the table is there, under the module's own schema.
    [Fact]
    public async Task Start_CreatesTheExamsTableInTheCatalogSchema()
    {
        await AdminAsync();

        var tables = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'catalog'")
            .ToListAsync());

        tables.Should().Contain("exams");
    }

    // AC3: without the permission the whole resource is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var list = await student.GetAsync(Exams);
        var one = await student.GetAsync($"{Exams}/{id}");
        var created = await student.PostAsJsonAsync(Exams, Valid(id), AppJson.Options);
        var updated = await student.PutAsJsonAsync($"{Exams}/{id}", Valid(id), AppJson.Options);
        var deleted = await student.DeleteAsync($"{Exams}/{id}");

        foreach (var response in new[] { list, one, created, updated, deleted })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    [Fact]
    public async Task List_Anonymous_IsUnauthorized()
    {
        (await Client().GetAsync(Exams)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC4: a national exam is created with no detail and comes back with its authority's name.
    [Fact]
    public async Task Create_NationalExam_StoresItWithoutADetailAndCarriesTheAuthorityName()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);

        var exam = await CreateAsync(admin, Valid(authority.Id, name: "Agente de Policia Federal"));

        exam.Name.Should().Be("Agente de Policia Federal");
        exam.ScopeDetail.Should().BeNull();
        exam.IssuingAuthorityId.Should().Be(authority.Id);
        exam.IssuingAuthorityName.Should().Be(authority.Name);
        exam.IssuingAuthorityAcronym.Should().Be(authority.Acronym);
        exam.ContentLanguage.Should().Be("pt-BR");

        var listed = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}");
        listed.Items.Should().ContainSingle(item => item.Id == exam.Id);
    }

    // AC5: the two scopes that need a place are refused without one, and nothing is written.
    [Theory]
    [InlineData(ExamScope.Municipal)]
    [InlineData(ExamScope.State)]
    public async Task Create_ScopeThatNeedsADetailWithoutOne_IsRefusedAndNothingIsWritten(ExamScope scope)
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);

        var response = await admin.PostAsJsonAsync(Exams, Valid(authority.Id, scope: scope), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamScopeDetailRequired);
        (await ListAsync(admin, $"?issuingAuthorityId={authority.Id}")).Total.Should().Be(0);
    }

    // AC6: the name is taken inside the authority, ignoring case and accents.
    [Theory]
    [InlineData("AGENTE DE TRANSITO")]
    [InlineData("agente de transito")]
    [InlineData("Agente de Trânsito")]
    public async Task Create_NameTakenInTheSameAuthorityIgnoringCaseAndAccents_IsRefused(string duplicate)
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id, name: "Agente de Transito"));

        var response = await admin.PostAsJsonAsync(Exams, Valid(authority.Id, name: duplicate), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNameTaken);
    }

    // AC7: the same name under two bodies is two different exams.
    [Fact]
    public async Task Create_SameNameUnderTwoAuthorities_CreatesBoth()
    {
        var admin = await AdminAsync();
        var first = await AuthorityAsync(admin);
        var second = await AuthorityAsync(admin);

        var one = await CreateAsync(admin, Valid(first.Id, name: "Agente"));
        var other = await CreateAsync(admin, Valid(second.Id, name: "Agente"));

        one.Id.Should().NotBe(other.Id);
        one.IssuingAuthorityId.Should().NotBe(other.IssuingAuthorityId);
    }

    // AC9: a parent that is not in the catalog, or one that was deleted, is the issuing authority's 404.
    [Fact]
    public async Task Create_IssuingAuthorityThatDoesNotExist_IsRefusedWithIssuingAuthorityNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync(Exams, Valid(Guid.CreateVersion7()), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNotFound);
    }

    [Fact]
    public async Task Create_IssuingAuthorityThatWasDeleted_IsRefusedWithIssuingAuthorityNotFound()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        (await admin.DeleteAsync($"{Authorities}/{authority.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.PostAsJsonAsync(Exams, Valid(authority.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNotFound);
    }

    // AC10: an unknown enum name is that field's own 400, never an uncoded deserialization failure.
    [Theory]
    [InlineData("Concurso", null, null, CatalogErrorCodes.ExamAssessmentTypeInvalid)]
    [InlineData(null, "Estadual", null, CatalogErrorCodes.ExamScopeInvalid)]
    [InlineData(null, null, "es-ES", CatalogErrorCodes.ExamContentLanguageInvalid)]
    public async Task Create_ValueThatIsNotOneOfTheNames_IsRefusedWithThatFieldsCode(
        string? assessmentType,
        string? scope,
        string? language,
        string expected)
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var request = new SaveExamRequest(
            authority.Id,
            Unique("Exame"),
            assessmentType ?? AssessmentType.Certification.ToString(),
            scope ?? ExamScope.National.ToString(),
            null,
            language ?? "pt-BR");

        var response = await admin.PostAsJsonAsync(Exams, request, AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(expected);
    }

    // AC11: the language is stored as the app writes it.
    [Fact]
    public async Task Create_LanguageInAnyCase_IsStoredCanonically()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);

        var exam = await CreateAsync(admin, Valid(authority.Id, contentLanguage: "pt-br"));

        exam.ContentLanguage.Should().Be("pt-BR");
    }

    // AC12: editing keeps the id and shows the change.
    [Fact]
    public async Task Update_NewNameAndScope_KeepsTheIdAndShowsTheChange()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var exam = await CreateAsync(admin, Valid(authority.Id, name: Unique("Antes")));

        var response = await admin.PutAsJsonAsync(
            $"{Exams}/{exam.Id}",
            Valid(authority.Id, name: "Guarda Municipal", scope: ExamScope.Municipal, scopeDetail: "Guarulhos (SP)"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updated = (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
        updated.Id.Should().Be(exam.Id);
        updated.Name.Should().Be("Guarda Municipal");
        updated.Scope.Should().Be(ExamScope.Municipal);
        updated.ScopeDetail.Should().Be("Guarulhos (SP)");
    }

    // AC12: an exam keeps its own name when it is saved again.
    [Fact]
    public async Task Update_ItsOwnName_IsNotATakenConflict()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var exam = await CreateAsync(admin, Valid(authority.Id, name: "FUVEST"));

        var response = await admin.PutAsJsonAsync(
            $"{Exams}/{exam.Id}",
            Valid(authority.Id, name: "FUVEST", assessmentType: AssessmentType.UniversityEntranceExam),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    // AC18c: the form page's read answers 404 for an id that is not an exam.
    [Fact]
    public async Task Find_AnIdThatIsNotAnExam_IsNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync($"{Exams}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNotFound);
    }

    [Fact]
    public async Task Find_AnExistingExam_ComesBackFilledForTheForm()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var exam = await CreateAsync(
            admin,
            Valid(authority.Id, scope: ExamScope.State, scopeDetail: "Sao Paulo", assessmentType: AssessmentType.UniversityEntranceExam));

        var found = (await admin.GetFromJsonAsync<ExamResponse>($"{Exams}/{exam.Id}", AppJson.Options))!;

        found.Should().Be(exam);
    }

    // AC13: the delete is a soft delete, and the name stays taken inside the authority.
    [Fact]
    public async Task Delete_ExistingExam_HidesItKeepsTheRowAndKeepsItsNameTaken()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var exam = await CreateAsync(admin, Valid(authority.Id, name: "Escrivao"));

        (await admin.DeleteAsync($"{Exams}/{exam.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(admin, $"?issuingAuthorityId={authority.Id}")).Items.Should().BeEmpty();
        (await admin.GetAsync($"{Exams}/{exam.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var row = await QueryAsync(context => context.Exams
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .FirstOrDefaultAsync(stored => stored.Id == exam.Id));
        row.Should().NotBeNull();
        row!.IsDeleted.Should().BeTrue();

        var again = await admin.PostAsJsonAsync(Exams, Valid(authority.Id, name: "escrivao"), AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNameTaken);
    }

    [Fact]
    public async Task Delete_AnIdThatIsNotAnExam_IsNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.DeleteAsync($"{Exams}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.ExamNotFound);
    }

    // AC14: an issuing authority with exams does not leave the catalog.
    [Fact]
    public async Task DeleteIssuingAuthority_ThatHasExams_IsRefusedAndItStays()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id));
        await CreateAsync(admin, Valid(authority.Id));

        var response = await admin.DeleteAsync($"{Authorities}/{authority.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityHasExams);

        var listed = await admin.GetFromJsonAsync<IssuingAuthorityPageResponse>(
            $"{Authorities}?search={Uri.EscapeDataString(authority.Acronym)}",
            AppJson.Options);
        listed!.Items.Should().ContainSingle(item => item.Id == authority.Id);
    }

    // AC15: once the exams are gone, the issuing authority goes too.
    [Fact]
    public async Task DeleteIssuingAuthority_WhoseOnlyExamWasDeleted_Succeeds()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var exam = await CreateAsync(admin, Valid(authority.Id));

        (await admin.DeleteAsync($"{Exams}/{exam.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.DeleteAsync($"{Authorities}/{authority.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // AC16: the search ignores accents, and the three filters combine with AND.
    [Fact]
    public async Task List_SearchWithoutAccents_FindsAccentedNames()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id, name: "Tecnico em Avaliação Judicial"));

        var found = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&search=avaliacao");

        found.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task List_TheThreeFiltersTogether_ReturnsOnlyWhatMatchesAllOfThem()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        var wanted = await CreateAsync(admin, Valid(
            authority.Id,
            assessmentType: AssessmentType.UniversityEntranceExam,
            scope: ExamScope.State,
            scopeDetail: "Sao Paulo"));
        await CreateAsync(admin, Valid(authority.Id, assessmentType: AssessmentType.Certification));
        await CreateAsync(admin, Valid(
            authority.Id,
            assessmentType: AssessmentType.UniversityEntranceExam,
            scope: ExamScope.Municipal,
            scopeDetail: "Guarulhos"));

        var found = await ListAsync(
            admin,
            $"?issuingAuthorityId={authority.Id}&assessmentType={AssessmentType.UniversityEntranceExam}&scope={ExamScope.State}");

        found.Total.Should().Be(1);
        found.Items.Should().ContainSingle(item => item.Id == wanted.Id);
    }

    // BR14: a filter value the server cannot read is no filter, never an error page.
    [Fact]
    public async Task List_FilterValueThatIsNotOneOfTheNames_IsIgnored()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id));

        var found = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&assessmentType=Concurso&scope=Estadual");

        found.Total.Should().Be(1);
    }

    // BR14: paging, with the full total.
    [Fact]
    public async Task List_MoreRowsThanOnePage_ReturnsThePageAndTheFullTotal()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        for (var index = 0; index < 3; index++)
        {
            await CreateAsync(admin, Valid(authority.Id, name: $"Exame {index}"));
        }

        var page = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&page=0&pageSize=2");

        page.Items.Should().HaveCount(2);
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task List_PageSizeOverTheCap_IsBroughtBackToTheCap()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id));

        var page = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&pageSize={ExamListQuery.MaxPageSize + 50}");

        page.Items.Count.Should().BeLessThanOrEqualTo(ExamListQuery.MaxPageSize);
    }

    // BR14: the default order is the name, ascending, read as the reader reads it.
    [Fact]
    public async Task List_WithoutASort_ComesBackByNameAscending()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id, name: "Zelador"));
        await CreateAsync(admin, Valid(authority.Id, name: "Ábaco"));
        await CreateAsync(admin, Valid(authority.Id, name: "Motorista"));

        var page = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}");

        page.Items.Select(item => item.Name).Should().Equal("Ábaco", "Motorista", "Zelador");
    }

    // BR14: the caller's order ranks the column whose label is translated.
    [Fact]
    public async Task List_SortedByScopeWithTheCallersOrder_FollowsIt()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin);
        await CreateAsync(admin, Valid(authority.Id, name: "A nacional"));
        await CreateAsync(admin, Valid(authority.Id, name: "B municipal", scope: ExamScope.Municipal, scopeDetail: "Guarulhos"));
        await CreateAsync(admin, Valid(authority.Id, name: "C estadual", scope: ExamScope.State, scopeDetail: "Sao Paulo"));

        var order = $"{ExamScope.Municipal},{ExamScope.State},{ExamScope.National}";
        var page = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&sortBy={ExamSort.Scope}&scopeOrder={order}");

        page.Items.Select(item => item.Scope).Should().Equal(ExamScope.Municipal, ExamScope.State, ExamScope.National);
    }

    [Fact]
    public async Task List_SortedByIssuingAuthority_OrdersByTheAuthorityName()
    {
        var admin = await AdminAsync();
        var first = await AuthorityAsync(admin);
        var second = await AuthorityAsync(admin);
        var one = await CreateAsync(admin, Valid(first.Id, name: Unique("Exame")));
        var other = await CreateAsync(admin, Valid(second.Id, name: Unique("Exame")));

        var ascending = await ListAsync(admin, $"?sortBy={ExamSort.IssuingAuthority}&pageSize={ExamListQuery.MaxPageSize}");
        var names = ascending.Items
            .Where(item => item.Id == one.Id || item.Id == other.Id)
            .Select(item => item.IssuingAuthorityName)
            .ToList();

        names.Should().Equal([.. names.OrderBy(name => name, StringComparer.Ordinal)]);
    }

    // BR2 (v2): an organizer is not a parent an exam can hang on - only an issuing authority is.
    [Fact]
    public async Task Create_UnderAnOrganizerId_IsRefused()
    {
        var admin = await AdminAsync();
        var organizer = await admin.PostAsJsonAsync(
            "/api/v1/catalog/organizers",
            new SaveOrganizerRequest(Unique("Banca"), Guid.CreateVersion7().ToString("N")[..12], OrganizerKind.ExamBoard.ToString()),
            AppJson.Options);
        var board = (await organizer.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;

        var response = await admin.PostAsJsonAsync(Exams, Valid(board.Id), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNotFound);
    }
}
