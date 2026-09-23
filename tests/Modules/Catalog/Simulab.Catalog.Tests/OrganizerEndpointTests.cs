using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-33 through HTTP, as the Web calls it. The tests of this class share one database, so each one
/// works on organizers with names of its own and never asserts a global count.
/// </summary>
public sealed class OrganizerEndpointTests : CatalogApiTests
{
    private const string Organizers = "/api/v1/catalog/organizers";

    private static string Unique(string name)
    {
        var value = $"{name} {Guid.CreateVersion7():N}";
        return value[..Math.Min(value.Length, 40)];
    }

    private static string UniqueAcronym() => Guid.CreateVersion7().ToString("N")[..12];

    private static SaveOrganizerRequest Valid(
        string? name = null,
        string? acronym = null,
        OrganizerKind kind = OrganizerKind.ExamBoard,
        string? description = null,
        string? website = null) =>
        new(name ?? Unique("Banca"), acronym ?? UniqueAcronym(), kind, description, website);

    private static async Task<OrganizerResponse> CreateAsync(HttpClient admin, SaveOrganizerRequest request)
    {
        var response = await admin.PostAsJsonAsync(Organizers, request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
    }

    private static async Task<OrganizerPageResponse> ListAsync(HttpClient admin, string query = "") =>
        (await admin.GetFromJsonAsync<OrganizerPageResponse>(Organizers + query, AppJson.Options))!;

    // AC2: the schema, the permission and the Admin grant exist after the host started.
    [Fact]
    public async Task Start_CreatesTheCatalogSchemaAndSeedsTheManagePermissionForAdmin()
    {
        var admin = await AdminAsync();

        var table = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'catalog'")
            .ToListAsync());
        var permissions = await admin.GetFromJsonAsync<List<PermissionName>>("/api/v1/identity/permissions", AppJson.Options);

        table.Should().Contain("organizers");
        permissions!.Select(permission => permission.Name).Should().Contain(CatalogPermissions.Manage);
        // The Admin who just signed in can call a route the permission guards, which is the grant in use.
        (await admin.GetAsync(Organizers)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC4: without the permission the whole module is closed.
    [Fact]
    public async Task EveryRoute_WithoutTheManagePermission_IsForbidden()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var list = await student.GetAsync(Organizers);
        var created = await student.PostAsJsonAsync(Organizers, Valid(), AppJson.Options);
        var updated = await student.PutAsJsonAsync($"{Organizers}/{id}", Valid(), AppJson.Options);
        var deleted = await student.DeleteAsync($"{Organizers}/{id}");

        foreach (var response in new[] { list, created, updated, deleted })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }

    [Fact]
    public async Task List_Anonymous_IsUnauthorized()
    {
        (await Client().GetAsync(Organizers)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC5: the acronym is stored uppercase and the row is listed.
    [Fact]
    public async Task Create_ValidData_StoresTheAcronymUppercasedAndListsIt()
    {
        var admin = await AdminAsync();
        var name = Unique("  Fundacao Getulio");
        var acronym = UniqueAcronym();

        var created = await CreateAsync(admin, Valid(name + "  ", acronym.ToLowerInvariant(), OrganizerKind.University, "Since 1944", "https://portal.fgv.br"));

        created.Name.Should().Be(name.Trim());
        created.Acronym.Should().Be(acronym.ToUpperInvariant());
        created.Kind.Should().Be(OrganizerKind.University);
        created.Description.Should().Be("Since 1944");
        created.Website.Should().Be("https://portal.fgv.br");
        (await ListAsync(admin, $"?search={Uri.EscapeDataString(name.Trim())}")).Items.Should().ContainSingle(item => item.Id == created.Id);
    }

    // AC6: case and accents do not make a new name.
    [Fact]
    public async Task Create_NameTakenIgnoringCaseAndAccents_IsRefusedWithNameTaken()
    {
        var admin = await AdminAsync();
        await CreateAsync(admin, Valid("Fundacao Carlos Chagas " + UniqueAcronym()));
        var taken = (await ListAsync(admin, "?search=Fundacao Carlos Chagas")).Items[0].Name;

        var response = await admin.PostAsJsonAsync(Organizers, Valid(taken.Replace("Fundacao", "FUNDAÇÃO", StringComparison.Ordinal)), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNameTaken);
    }

    // AC7: the acronym is unique in any case.
    [Fact]
    public async Task Create_AcronymTakenInAnyCase_IsRefusedWithAcronymTaken()
    {
        var admin = await AdminAsync();
        var acronym = UniqueAcronym();
        await CreateAsync(admin, Valid(acronym: acronym.ToUpperInvariant()));

        var response = await admin.PostAsJsonAsync(Organizers, Valid(acronym: acronym.ToLowerInvariant()), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerAcronymTaken);
    }

    [Fact]
    public async Task Update_ItsOwnNameAndAcronym_IsNotATakenConflict()
    {
        var admin = await AdminAsync();
        var organizer = await CreateAsync(admin, Valid());

        var response = await admin.PutAsJsonAsync(
            $"{Organizers}/{organizer.Id}",
            new SaveOrganizerRequest(organizer.Name.ToUpperInvariant(), organizer.Acronym, OrganizerKind.CertifyingBody, null, null),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<OrganizerResponse>(AppJson.Options))!;
        updated.Id.Should().Be(organizer.Id, "editing never replaces the row");
        updated.Kind.Should().Be(OrganizerKind.CertifyingBody);
    }

    // AC9: the edit shows on the list and keeps the id.
    [Fact]
    public async Task Update_NewName_ShowsOnTheListUnderTheSameId()
    {
        var admin = await AdminAsync();
        var organizer = await CreateAsync(admin, Valid());
        var renamed = Unique("Renamed");

        var response = await admin.PutAsJsonAsync(
            $"{Organizers}/{organizer.Id}",
            new SaveOrganizerRequest(renamed, organizer.Acronym, organizer.Kind, null, null),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = (await ListAsync(admin, $"?search={Uri.EscapeDataString(renamed)}")).Items.Should().ContainSingle().Subject;
        listed.Id.Should().Be(organizer.Id);
        listed.Name.Should().Be(renamed);
    }

    [Fact]
    public async Task Update_UnknownId_IsNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.PutAsJsonAsync($"{Organizers}/{Guid.CreateVersion7()}", Valid(), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNotFound);
    }

    // AC10: the delete is a soft delete and the name stays taken.
    [Fact]
    public async Task Delete_ExistingOrganizer_HidesItKeepsTheRowAndKeepsItsNameTaken()
    {
        var admin = await AdminAsync();
        var request = Valid();
        var organizer = await CreateAsync(admin, request);

        var deleted = await admin.DeleteAsync($"{Organizers}/{organizer.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(admin, $"?search={Uri.EscapeDataString(organizer.Name)}")).Items.Should().BeEmpty();

        var row = await QueryAsync(context => context.Organizers
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(item => item.Id == organizer.Id));
        row.IsDeleted.Should().BeTrue();

        var again = await admin.PostAsJsonAsync(Organizers, request with { Acronym = UniqueAcronym() }, AppJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNameTaken);
    }

    [Fact]
    public async Task Delete_UnknownId_IsNotFound()
    {
        var admin = await AdminAsync();

        var response = await admin.DeleteAsync($"{Organizers}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNotFound);
    }

    // AC11: the website is checked by the Api, which is the authority.
    [Fact]
    public async Task Create_WebsiteThatIsNotAnAbsoluteWebAddress_IsRefusedAndNothingIsWritten()
    {
        var admin = await AdminAsync();
        var request = Valid(website: "cebraspe.org.br");

        var response = await admin.PostAsJsonAsync(Organizers, request, AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerWebsiteInvalid);
        (await ListAsync(admin, $"?search={Uri.EscapeDataString(request.Name!)}")).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", CatalogErrorCodes.OrganizerNameRequired)]
    [InlineData("  ", CatalogErrorCodes.OrganizerNameRequired)]
    public async Task Create_BlankName_IsRefusedWithItsOwnCode(string name, string code)
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync(Organizers, Valid(name: name), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
    }

    // A blank name and a taken acronym at once: the answer is about the shape, not the conflict.
    [Fact]
    public async Task Create_BlankNameAndTakenAcronym_AnswersTheShapeFirst()
    {
        var admin = await AdminAsync();
        var existing = await CreateAsync(admin, Valid());

        var response = await admin.PostAsJsonAsync(Organizers, new SaveOrganizerRequest(" ", existing.Acronym, OrganizerKind.ExamBoard), AppJson.Options);

        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.OrganizerNameRequired);
    }

    // AC12: the search ignores accents, in both directions.
    [Fact]
    public async Task List_SearchWithoutAccents_FindsAccentedNames()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        // Two different names, one accented and one not: BR9 would refuse two that differ only by accents.
        await CreateAsync(admin, Valid(name: $"{marker} Fundação"));
        await CreateAsync(admin, Valid(name: $"{marker} Fundacao Sul"));

        var withoutAccent = await ListAsync(admin, $"?search={marker} fundacao");
        var withAccent = await ListAsync(admin, $"?search={Uri.EscapeDataString($"{marker} FUNDAÇÃO")}");

        withoutAccent.Items.Should().HaveCount(2);
        withAccent.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_SearchByAcronym_FindsTheOrganizer()
    {
        var admin = await AdminAsync();
        var organizer = await CreateAsync(admin, Valid());

        var found = await ListAsync(admin, $"?search={organizer.Acronym.ToLowerInvariant()}");

        found.Items.Should().ContainSingle(item => item.Id == organizer.Id);
    }

    // AC13: the page is a page and the total counts everything the filter matched.
    [Fact]
    public async Task List_MoreRowsThanOnePage_ReturnsThePageAndTheFullTotal()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        for (var index = 0; index < 3; index++)
        {
            await CreateAsync(admin, Valid(name: $"Banca {marker} {index}"));
        }

        var first = await ListAsync(admin, $"?search={marker}&page=0&pageSize=2");
        var second = await ListAsync(admin, $"?search={marker}&page=1&pageSize=2");

        first.Items.Should().HaveCount(2);
        first.Total.Should().Be(3);
        second.Items.Should().ContainSingle();
        second.Total.Should().Be(3);
        first.Items.Select(item => item.Id).Should().NotIntersectWith(second.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task List_PageSizeOverTheCap_IsBroughtBackToTheCap()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        await CreateAsync(admin, Valid(name: $"Banca {marker}"));

        var page = await ListAsync(admin, $"?search={marker}&pageSize=5000");

        page.Items.Should().ContainSingle("the cap changes how many rows may come back, not which ones match");
    }

    [Fact]
    public async Task List_SortedByAcronymDescending_ReversesTheOrder()
    {
        var admin = await AdminAsync();
        var marker = UniqueAcronym();
        await CreateAsync(admin, Valid(name: $"Banca A {marker}", acronym: $"a{marker}"));
        await CreateAsync(admin, Valid(name: $"Banca B {marker}", acronym: $"b{marker}"));

        var ascending = await ListAsync(admin, $"?search={marker}&sortBy=acronym");
        var descending = await ListAsync(admin, $"?search={marker}&sortBy=acronym&descending=true");

        ascending.Items.Select(item => item.Acronym).Should().Equal(descending.Items.Select(item => item.Acronym).Reverse());
        ascending.Items[0].Acronym.Should().Be($"A{marker}".ToUpperInvariant());
    }

    /// <summary>Just the name of a permission, so this module does not depend on Identity's response type.</summary>
    private sealed record PermissionName(string Name);
}
