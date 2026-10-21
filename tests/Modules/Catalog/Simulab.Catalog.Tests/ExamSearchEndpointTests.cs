using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-80 through HTTP, as the Web calls it: the back-office exam search reads the exam name, the issuing
/// authority name and the scope detail, word by word, like the student catalog (F-36 BR3). Each test owns a
/// token carried by its authority and exam names, so it never depends on what the other tests created.
/// </summary>
public sealed class ExamSearchEndpointTests : CatalogApiTests
{
    private const string Exams = "/api/v1/catalog/exams";
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    private static string Token() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<IssuingAuthorityResponse> AuthorityAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync(Authorities, new SaveIssuingAuthorityRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!;
    }

    private static async Task<ExamResponse> ExamAsync(
        HttpClient admin,
        Guid authority,
        string name,
        ExamScope scope = ExamScope.National,
        string? detail = null,
        AssessmentType assessmentType = AssessmentType.PublicServiceExam)
    {
        var request = new SaveExamRequest(authority, name, assessmentType.ToString(), scope.ToString(), detail, "pt-BR");
        var response = await admin.PostAsJsonAsync(Exams, request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ExamResponse>(AppJson.Options))!;
    }

    private static async Task<ExamPageResponse> ListAsync(HttpClient admin, string query) =>
        (await admin.GetFromJsonAsync<ExamPageResponse>(Exams + query, AppJson.Options))!;

    private static string Search(string text, string more = "") => $"?search={Uri.EscapeDataString(text)}{more}";

    // AC1 (UC1, BR1): a word of the authority's name finds its exams, whatever the case or the accents.
    [Fact]
    public async Task List_SearchByAnAuthorityWord_FindsTheExamsOfThatAuthority()
    {
        var admin = await AdminAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Tribunal de Justiça Alfa {token}");
        var exam = await ExamAsync(admin, authority.Id, "Tecnico Judiciario");
        await ExamAsync(admin, (await AuthorityAsync(admin, $"Outro Orgao {token}")).Id, "Analista");

        foreach (var typed in new[] { $"alfa {token}", $"ALFA {token}", $"justica alfa {token}", $"JUSTIÇA {token}" })
        {
            (await ListAsync(admin, Search(typed))).Items.Should().ContainSingle(item => item.Id == exam.Id, typed);
        }
    }

    // AC2 (UC2, BR3): a State exam is found by its state's acronym and name.
    [Fact]
    public async Task List_SearchByTheStateAcronymOrName_FindsTheStateExam()
    {
        var admin = await AdminAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Banca {token}");
        var exam = await ExamAsync(admin, authority.Id, "Escrevente", ExamScope.State, "SP");

        foreach (var typed in new[] { $"sp {token}", $"sao paulo {token}", $"SÃO PAULO {token}" })
        {
            (await ListAsync(admin, Search(typed))).Items.Should().ContainSingle(item => item.Id == exam.Id, typed);
        }

        (await ListAsync(admin, Search($"rio {token}"))).Items.Should().BeEmpty();
    }

    // AC3 (UC3, BR3): a Municipal exam is found by its place; a National exam has none.
    [Fact]
    public async Task List_SearchByTheMunicipality_FindsTheMunicipalExamAndNeverANationalOne()
    {
        var admin = await AdminAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Prefeitura {token}");
        var municipal = await ExamAsync(admin, authority.Id, "Guarda", ExamScope.Municipal, "Guarulhos");
        await ExamAsync(admin, authority.Id, "Analista");

        (await ListAsync(admin, Search($"guarulhos {token}"))).Items.Should().ContainSingle().Which.Id.Should().Be(municipal.Id);
        (await ListAsync(admin, Search($"sp {token}"))).Items.Should().BeEmpty("a National exam has no scope detail");
    }

    // AC4 (UC4, BR1): every word must appear somewhere, each in any of the three fields.
    [Fact]
    public async Task List_SearchWithWordsOfDifferentFields_FindsTheExamThatHasEachWordSomewhere()
    {
        var admin = await AdminAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Prefeitura de São Paulo {token}");
        var exam = await ExamAsync(admin, authority.Id, "Guarda Municipal", ExamScope.State, "SP");

        foreach (var typed in new[] { $"guarda sao {token}", $"GUARDA  paulo   {token}", $"guarda sp {token}" })
        {
            (await ListAsync(admin, Search(typed))).Items.Should().ContainSingle(item => item.Id == exam.Id, typed);
        }

        (await ListAsync(admin, Search($"guarda rio {token}"))).Items.Should().BeEmpty();
    }

    // AC5 (BR1, BR5): the words of a name no longer have to be contiguous or in order.
    [Fact]
    public async Task List_SearchWithNonContiguousWordsOfTheName_FindsTheExam()
    {
        var admin = await AdminAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Orgao {token}");
        var exam = await ExamAsync(admin, authority.Id, "Tecnico em Avaliação Judicial");

        foreach (var typed in new[] { $"avaliacao {token}", $"judicial tecnico {token}" })
        {
            (await ListAsync(admin, Search(typed))).Items.Should().ContainSingle(item => item.Id == exam.Id, typed);
        }
    }

    // AC6 (BR2): the stored acronym of an authority is a hidden value; it is not searched.
    [Fact]
    public async Task List_SearchByTheStoredAcronymOfTheAuthority_FindsNothing()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin, $"Orgao {Token()}");
        var acronym = $"XY{Token()[..8]}";
        await StoreAcronymAsync(authority.Id, acronym);
        await ExamAsync(admin, authority.Id, "Tecnico");

        (await ListAsync(admin, Search(acronym.ToLowerInvariant()))).Items.Should().BeEmpty();
    }

    // AC7 (UC5, BR4): the search combines with the filters by AND, and `total` and the paging count only the matches.
    [Fact]
    public async Task List_SearchWithFiltersAndPaging_CountsOnlyWhatTheSearchAndTheFiltersMatched()
    {
        var admin = await AdminAsync();
        var token = Token();
        var first = await AuthorityAsync(admin, $"Alfa {token}");
        var second = await AuthorityAsync(admin, $"Beta {token}");
        await ExamAsync(admin, first.Id, "Um", assessmentType: AssessmentType.Certification);
        await ExamAsync(admin, first.Id, "Dois", assessmentType: AssessmentType.Certification);
        await ExamAsync(admin, first.Id, "Tres");
        await ExamAsync(admin, second.Id, "Quatro", assessmentType: AssessmentType.Certification);

        var both = await ListAsync(admin, Search(token, "&pageSize=1&page=0"));
        both.Total.Should().Be(4);

        var filtered = Search($"alfa {token}", $"&issuingAuthorityId={first.Id}&assessmentType={AssessmentType.Certification}&pageSize=1&page=0");
        var firstPage = await ListAsync(admin, filtered);
        var secondPage = await ListAsync(admin, filtered.Replace("page=0", "page=1", StringComparison.Ordinal));

        firstPage.Total.Should().Be(2);
        firstPage.Items.Should().ContainSingle();
        secondPage.Total.Should().Be(2);
        secondPage.Items.Should().ContainSingle().Which.Id.Should().NotBe(firstPage.Items[0].Id);
    }

    // AC8 (BR1): no text and only spaces are no filter at all.
    [Fact]
    public async Task List_SearchBlankOrAbsent_ReturnsEveryExam()
    {
        var admin = await AdminAsync();
        var authority = await AuthorityAsync(admin, $"Orgao {Token()}");
        await ExamAsync(admin, authority.Id, "Um");
        await ExamAsync(admin, authority.Id, "Dois");

        var all = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}");
        var blank = await ListAsync(admin, $"?issuingAuthorityId={authority.Id}&search=%20%20%20");

        all.Total.Should().Be(2);
        blank.Total.Should().Be(2);
        blank.Items.Select(item => item.Id).Should().BeEquivalentTo(all.Items.Select(item => item.Id));
    }

    // AC9 (UC5, BR6): the same route answers a student with 403 and an admin with the matches.
    [Fact]
    public async Task List_SearchThroughTheHttpPipeline_ServesTheAdminAndClosesTheStudent()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var token = Token();
        var authority = await AuthorityAsync(admin, $"Tribunal Gama {token}");
        var exam = await ExamAsync(admin, authority.Id, "Tecnico");

        var forbidden = await student.GetAsync(Exams + Search($"gama {token}"));
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await forbidden.Content.ReadAsStringAsync()).Should().Contain(IdentityErrorCodes.Forbidden);

        (await ListAsync(admin, Search($"gama {token}"))).Items.Should().ContainSingle().Which.Id.Should().Be(exam.Id);
    }
}
