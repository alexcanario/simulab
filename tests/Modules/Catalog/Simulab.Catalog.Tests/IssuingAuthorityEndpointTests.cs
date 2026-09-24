using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-34 BR18 (v2) through HTTP: the back office of the body that publishes a notice. The tests share one
/// database, so each works on rows of its own and never asserts a global count.
/// </summary>
public sealed class IssuingAuthorityEndpointTests : CatalogApiTests
{
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static string UniqueAcronym() => Guid.CreateVersion7().ToString("N")[..12];

    private static SaveIssuingAuthorityRequest Valid(
        string? name = null,
        string? acronym = null,
        string? description = null,
        string? website = null) =>
        new(name ?? Unique("Orgao"), acronym ?? UniqueAcronym(), description, website);

    private static async Task<IssuingAuthorityResponse> CreateAsync(HttpClient admin, SaveIssuingAuthorityRequest request)
    {
        var response = await admin.PostAsJsonAsync(Authorities, request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<IssuingAuthorityResponse>(AppJson.Options))!;
    }

    private static async Task<IssuingAuthorityPageResponse> ListAsync(HttpClient admin, string query = "") =>
        (await admin.GetFromJsonAsync<IssuingAuthorityPageResponse>(Authorities + query, AppJson.Options))!;

    // AC17 (v2): the table is there, under the module's own schema.
    [Fact]
    public async Task Start_CreatesTheIssuingAuthoritiesTableInTheCatalogSchema()
    {
        await AdminAsync();

        var tables = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'catalog'")
            .ToListAsync());

        tables.Should().Contain("issuing_authorities").And.Contain("organizers", "the board keeps its own table");
    }

    // AC17 (v2): without the permission the whole resource is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var list = await student.GetAsync(Authorities);
        var created = await student.PostAsJsonAsync(Authorities, Valid(), AppJson.Options);
        var updated = await student.PutAsJsonAsync($"{Authorities}/{id}", Valid(), AppJson.Options);
        var deleted = await student.DeleteAsync($"{Authorities}/{id}");

        foreach (var response in new[] { list, created, updated, deleted })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    [Fact]
    public async Task List_Anonymous_IsUnauthorized()
    {
        (await Client().GetAsync(Authorities)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC17 (v2): create, with the acronym uppercased, and it is listed.
    [Fact]
    public async Task Create_ValidData_StoresTheAcronymUppercasedAndListsIt()
    {
        var admin = await AdminAsync();
        var acronym = UniqueAcronym();

        var created = await CreateAsync(admin, Valid(name: Unique("Prefeitura"), acronym: acronym));

        created.Acronym.Should().Be(acronym.ToUpperInvariant());
        var listed = await ListAsync(admin, $"?search={acronym}");
        listed.Items.Should().ContainSingle(item => item.Id == created.Id);
    }

    // AC17 (v2): the name is taken, ignoring case and accents.
    [Fact]
    public async Task Create_NameTakenIgnoringCaseAndAccents_IsRefusedWithNameTaken()
    {
        var admin = await AdminAsync();
        var name = $"Prefeitura de Sao Paulo {Guid.CreateVersion7():N}"[..40];
        await CreateAsync(admin, Valid(name: name));

        var response = await admin.PostAsJsonAsync(
            Authorities,
            Valid(name: name.Replace("Sao Paulo", "São Paulo", StringComparison.Ordinal).ToUpperInvariant()),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNameTaken);
    }

    [Fact]
    public async Task Create_AcronymTakenInAnyCase_IsRefusedWithAcronymTaken()
    {
        var admin = await AdminAsync();
        var acronym = UniqueAcronym();
        await CreateAsync(admin, Valid(acronym: acronym));

        var response = await admin.PostAsJsonAsync(Authorities, Valid(acronym: acronym.ToUpperInvariant()), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityAcronymTaken);
    }

    [Fact]
    public async Task Create_BlankName_IsRefusedWithItsOwnCode()
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync(Authorities, Valid(name: " "), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNameRequired);
    }

    [Fact]
    public async Task Create_WebsiteThatIsNotAnAbsoluteWebAddress_IsRefusedAndNothingIsWritten()
    {
        var admin = await AdminAsync();
        var acronym = UniqueAcronym();

        var response = await admin.PostAsJsonAsync(Authorities, Valid(acronym: acronym, website: "guarulhos.sp.gov.br"), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityWebsiteInvalid);
        (await ListAsync(admin, $"?search={acronym}")).Total.Should().Be(0);
    }

    [Fact]
    public async Task Update_NewName_ShowsOnTheListUnderTheSameId()
    {
        var admin = await AdminAsync();
        var acronym = UniqueAcronym();
        var created = await CreateAsync(admin, Valid(acronym: acronym));
        var renamed = Unique("Ministerio");

        var response = await admin.PutAsJsonAsync(
            $"{Authorities}/{created.Id}",
            Valid(name: renamed, acronym: acronym),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var listed = await ListAsync(admin, $"?search={acronym}");
        listed.Items.Should().ContainSingle(item => item.Id == created.Id && item.Name == renamed);
    }

    [Fact]
    public async Task Update_AnIdThatIsNotAnIssuingAuthority_IsNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.PutAsJsonAsync($"{Authorities}/{Guid.CreateVersion7()}", Valid(), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNotFound);
    }

    // The delete is a soft delete, and the name stays taken.
    [Fact]
    public async Task Delete_ExistingOne_HidesItKeepsTheRowAndKeepsItsNameTaken()
    {
        var admin = await AdminAsync();
        var name = Unique("Autarquia");
        var acronym = UniqueAcronym();
        var created = await CreateAsync(admin, Valid(name: name, acronym: acronym));

        (await admin.DeleteAsync($"{Authorities}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(admin, $"?search={acronym}")).Items.Should().BeEmpty();

        var row = await QueryAsync(context => context.IssuingAuthorities
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .FirstOrDefaultAsync(stored => stored.Id == created.Id));
        row.Should().NotBeNull();
        row!.IsDeleted.Should().BeTrue();

        var again = await admin.PostAsJsonAsync(Authorities, Valid(name: name), AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNameTaken);
    }

    // BR18: the search ignores case and accents, over both columns.
    [Fact]
    public async Task List_SearchWithoutAccents_FindsAccentedNames()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        await CreateAsync(admin, Valid(name: $"Ministério da Educação {marker}", acronym: marker));

        var found = await ListAsync(admin, $"?search={Uri.EscapeDataString("educacao")}");

        found.Items.Should().Contain(item => item.Acronym == marker.ToUpperInvariant());
    }

    [Fact]
    public async Task List_MoreRowsThanOnePage_ReturnsThePageAndTheFullTotal()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        for (var index = 0; index < 3; index++)
        {
            await CreateAsync(admin, Valid(name: $"Orgao {marker} {index}", acronym: UniqueAcronym()));
        }

        var page = await ListAsync(admin, $"?search={marker}&page=0&pageSize=2");

        page.Items.Should().HaveCount(2);
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task List_PageSizeOverTheCap_IsBroughtBackToTheCap()
    {
        var admin = await AdminAsync();
        await CreateAsync(admin, Valid());

        var page = await ListAsync(admin, $"?pageSize={IssuingAuthorityListQuery.MaxPageSize + 50}");

        page.Items.Count.Should().BeLessThanOrEqualTo(IssuingAuthorityListQuery.MaxPageSize);
    }

    [Fact]
    public async Task List_SortedByAcronym_ComesBackInThatOrder()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym()[..8];
        await CreateAsync(admin, Valid(name: $"Orgao {marker} B", acronym: $"{marker}bb"));
        await CreateAsync(admin, Valid(name: $"Orgao {marker} A", acronym: $"{marker}aa"));

        var page = await ListAsync(admin, $"?search={marker}&sortBy={IssuingAuthoritySort.Acronym}");

        page.Items.Select(item => item.Acronym).Should().Equal($"{marker}aa".ToUpperInvariant(), $"{marker}bb".ToUpperInvariant());
    }
}
