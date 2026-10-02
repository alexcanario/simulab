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
/// database, so each works on rows of its own and never asserts a global count. F-44 took the acronym off
/// the contract: a stored one is written straight to the table, as the rows typed earlier hold it.
/// </summary>
public sealed class IssuingAuthorityEndpointTests : CatalogApiTests
{
    private const string Authorities = "/api/v1/catalog/issuing-authorities";

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static SaveIssuingAuthorityRequest Valid(
        string? name = null,
        string? description = null,
        string? website = null) =>
        new(name ?? Unique("Orgao"), description, website);

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

    // F-44 AC2: create asks for no acronym, stores none, and the authority is listed.
    [Fact]
    public async Task Create_ValidData_StoresNoAcronymAndListsIt()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var created = await CreateAsync(admin, Valid(name: $"Prefeitura {marker}"));

        var stored = await QueryAsync(context => context.IssuingAuthorities.AsNoTracking().SingleAsync(row => row.Id == created.Id));
        stored.Acronym.Should().BeNull();
        stored.NormalizedAcronym.Should().BeNull();
        var listed = await ListAsync(admin, $"?search={marker}");
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

    // F-44 AC4: the acronym is no longer unique, so two authorities with no acronym coexist.
    [Fact]
    public async Task Create_TwoAuthoritiesWithNoAcronym_AreBothAccepted()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        await CreateAsync(admin, Valid(name: $"Orgao {marker} A"));
        await CreateAsync(admin, Valid(name: $"Orgao {marker} B"));

        (await ListAsync(admin, $"?search={marker}")).Total.Should().Be(2);
    }

    // F-44 AC4: the table accepts two authorities holding the same stored acronym.
    [Fact]
    public async Task StoredAcronym_TwoAuthoritiesWithTheSameOne_IsAccepted()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var first = await CreateAsync(admin, Valid(name: $"Orgao {marker} A"));
        var second = await CreateAsync(admin, Valid(name: $"Orgao {marker} B"));

        await StoreAcronymAsync(first.Id, marker);
        var act = () => StoreAcronymAsync(second.Id, marker);

        await act.Should().NotThrowAsync("the unique index over the acronym is gone");
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
        var marker = Marker();

        var response = await admin.PostAsJsonAsync(Authorities, Valid(name: $"Orgao {marker}", website: "guarulhos.sp.gov.br"), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityWebsiteInvalid);
        (await ListAsync(admin, $"?search={marker}")).Total.Should().Be(0);
    }

    [Fact]
    public async Task Update_NewName_ShowsOnTheListUnderTheSameId()
    {
        var admin = await AdminAsync();
        var created = await CreateAsync(admin, Valid());
        var renamed = Unique("Ministerio");

        var response = await admin.PutAsJsonAsync($"{Authorities}/{created.Id}", Valid(name: renamed), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var listed = await ListAsync(admin, $"?search={Uri.EscapeDataString(renamed)}");
        listed.Items.Should().ContainSingle(item => item.Id == created.Id && item.Name == renamed);
    }

    // F-44 AC3: an edit keeps the acronym stored for the authority.
    [Fact]
    public async Task Update_AnAuthorityWithAStoredAcronym_KeepsItUnchanged()
    {
        var admin = await AdminAsync();
        var created = await CreateAsync(admin, Valid());
        await StoreAcronymAsync(created.Id, "INSS");

        var response = await admin.PutAsJsonAsync($"{Authorities}/{created.Id}", Valid(name: Unique("Instituto")), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var stored = await QueryAsync(context => context.IssuingAuthorities.AsNoTracking().SingleAsync(row => row.Id == created.Id));
        stored.Acronym.Should().Be("INSS");
        stored.NormalizedAcronym.Should().Be("INSS");
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
        var created = await CreateAsync(admin, Valid(name: name));

        (await admin.DeleteAsync($"{Authorities}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(admin, $"?search={Uri.EscapeDataString(name)}")).Items.Should().BeEmpty();

        var row = await QueryAsync(context => context.IssuingAuthorities
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .FirstOrDefaultAsync(stored => stored.Id == created.Id));
        row.Should().NotBeNull();
        row!.IsDeleted.Should().BeTrue();

        var again = await admin.PostAsJsonAsync(Authorities, Valid(name: name), AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.IssuingAuthorityNameTaken);
    }

    // BR18: the search ignores case and accents.
    [Fact]
    public async Task List_SearchWithoutAccents_FindsAccentedNames()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var created = await CreateAsync(admin, Valid(name: $"Ministério da Educação {marker}"));

        var found = await ListAsync(admin, $"?search={Uri.EscapeDataString("educacao " + marker)}");

        found.Items.Select(item => item.Id).Should().Contain(created.Id);
    }

    // F-44 AC7: a stored acronym is not searched; the name still finds the authority.
    [Fact]
    public async Task List_SearchByAStoredAcronym_DoesNotFindIt_ButTheNameDoes()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var created = await CreateAsync(admin, Valid(name: $"Instituto Nacional do Seguro Social {marker}"));
        await StoreAcronymAsync(created.Id, $"IN{marker[..8]}");

        (await ListAsync(admin, $"?search=IN{marker[..8]}")).Items.Should().BeEmpty();
        (await ListAsync(admin, $"?search=Seguro Social {marker}")).Items.Should().ContainSingle(item => item.Id == created.Id);
    }

    [Fact]
    public async Task List_MoreRowsThanOnePage_ReturnsThePageAndTheFullTotal()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        for (var index = 0; index < 3; index++)
        {
            await CreateAsync(admin, Valid(name: $"Orgao {marker} {index}"));
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

    // F-44 BR6: there is no acronym sort; an unknown value sorts by name.
    [Fact]
    public async Task List_SortedByAnAcronymColumn_FallsBackToTheNameOrder()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        await CreateAsync(admin, Valid(name: $"Orgao {marker} B"));
        await CreateAsync(admin, Valid(name: $"Orgao {marker} A"));

        var page = await ListAsync(admin, $"?search={marker}&sortBy=acronym");

        page.Items.Select(item => item.Name).Should().Equal($"Orgao {marker} A", $"Orgao {marker} B");
    }
}
