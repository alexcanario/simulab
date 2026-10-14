using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-77 through HTTP, subject side: the other names a subject goes by travel in the save request as a whole
/// set. The tests share one database, so each works on rows of its own, found by a marker in the text.
/// </summary>
public sealed class SubjectAliasEndpointTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient admin,
        string name,
        params string[]? aliases) =>
        await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name, null, aliases), AppJson.Options);

    private static async Task<SubjectResponse> CreateAsync(HttpClient admin, string name, params string[] aliases)
    {
        var response = await PostAsync(admin, name, aliases);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
    }

    private static async Task<HttpResponseMessage> PutAsync(
        HttpClient admin,
        Guid id,
        string name,
        IReadOnlyList<string>? aliases) =>
        await admin.PutAsJsonAsync($"{Subjects}/{id}", new SaveSubjectRequest(name, null, aliases), AppJson.Options);

    private static async Task<SubjectResponse> FindAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<SubjectResponse>($"{Subjects}/{id}", AppJson.Options))!;

    private static async Task<SubjectPageResponse> ListAsync(HttpClient admin, string query) =>
        (await admin.GetFromJsonAsync<SubjectPageResponse>($"{Subjects}?{query}", AppJson.Options))!;

    private Task<List<bool>> StoredDeletedFlagsAsync(Guid subjectId) =>
        QueryAsync(context => context.Database
            .SqlQuery<bool>($"""
                SELECT is_deleted AS "Value" FROM catalog.subject_aliases
                WHERE subject_id = {subjectId} ORDER BY normalized_name
                """)
            .ToListAsync());

    // AC2: the repeated name counts once, the first as typed; the response and a later read carry one alias.
    [Fact]
    public async Task Create_WithTheSameNameTwiceByNormalizedForm_StoresOneAliasAndShowsOneChip()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var created = await CreateAsync(
            admin, $"Língua Portuguesa {marker}", $"Português {marker}", $"Portugues {marker}");

        created.Aliases.Should().Equal($"Português {marker}");
        (await FindAsync(admin, created.Id)).Aliases.Should().Equal($"Português {marker}");
        (await StoredDeletedFlagsAsync(created.Id)).Should().Equal(false);
    }

    // BR13: the aliases come back ordered by their normalized form, on the list rows too.
    [Fact]
    public async Task Aliases_AreOrderedByNormalizedForm_OnTheSubjectAndOnTheListRow()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var created = await CreateAsync(admin, $"Direito {marker}", $"Zeta {marker}", $"Álgebra {marker}", $"Beta {marker}");

        var expected = new[] { $"Álgebra {marker}", $"Beta {marker}", $"Zeta {marker}" };
        created.Aliases.Should().Equal(expected);
        (await ListAsync(admin, $"search={marker}")).Items.Should().ContainSingle()
            .Which.Aliases.Should().Equal(expected);
    }

    // AC3: 1 and 151 characters are refused with their own code and nothing is written.
    [Theory]
    [InlineData(1, CatalogErrorCodes.SubjectAliasTooShort)]
    [InlineData(151, CatalogErrorCodes.SubjectAliasTooLong)]
    public async Task Create_AliasOutOfRange_IsRefused_AndNothingIsWritten(int length, string code)
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var response = await PostAsync(admin, $"Matemática {marker}", new string('x', length));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        (await ListAsync(admin, $"search={marker}")).Total.Should().Be(0);
    }

    // AC4: equal to its own name once normalized.
    [Fact]
    public async Task Create_AliasEqualToTheName_IsRefused_AndNothingIsWritten()
    {
        var admin = await AdminAsync();
        var marker = Marker();

        var response = await PostAsync(admin, $"Língua Portuguesa {marker}", $"lingua portuguesa {marker}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectAliasSameAsName);
        (await ListAsync(admin, $"search={marker}")).Total.Should().Be(0);
    }

    // AC4 (design pass): the check runs against the resulting set, so renaming a subject to one of its own
    // stored aliases, with the aliases left null, is the same refusal.
    [Fact]
    public async Task Update_RenamedToItsOwnStoredAlias_WithAliasesNull_IsRefusedAsSameAsName()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");

        var response = await PutAsync(admin, subject.Id, $"PORTUGUES {marker}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectAliasSameAsName);
        (await FindAsync(admin, subject.Id)).Name.Should().Be($"Língua Portuguesa {marker}");
    }

    // AC5: unique across the whole subject vocabulary, in both directions.
    [Fact]
    public async Task Alias_ClashingWithAnotherSubjectsNameOrAlias_IsRefused_AndSoIsTheReverse()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");
        var other = await CreateAsync(admin, $"Matemática {marker}");

        var byAlias = await PutAsync(admin, other.Id, other.Name, [$"portugues {marker}"]);
        var byName = await PutAsync(admin, other.Id, other.Name, [$"lingua portuguesa {marker}"]);
        var reverse = await PostAsync(admin, $"Português {marker}");

        foreach (var response in new[] { byAlias, byName })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectAliasTaken);
        }

        reverse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await reverse.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameTaken);
        (await FindAsync(admin, other.Id)).Aliases.Should().BeEmpty();
    }

    // AC5 (BR6, reverse by rename): renaming a subject to another subject's alias is the name conflict.
    [Fact]
    public async Task Rename_ToAnotherSubjectsLiveAlias_IsRefusedAsNameTaken()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");
        var other = await CreateAsync(admin, $"Matemática {marker}");

        var response = await PutAsync(admin, other.Id, $"PORTUGUÊS {marker}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.SubjectNameTaken);
    }

    // AC7: a removed alias frees its text, and its old row stays flagged.
    [Fact]
    public async Task RemovedAlias_FreesItsText_ForAnotherSubjectAsAliasAndAsName_AndTheRowStaysFlagged()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var first = await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");
        var second = await CreateAsync(admin, $"Matemática {marker}");

        var removed = await PutAsync(admin, first.Id, first.Name, []);
        var asAlias = await PutAsync(admin, second.Id, second.Name, [$"Português {marker}"]);
        var asName = await PostAsync(admin, $"Portugues {marker}");

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await removed.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Aliases.Should().BeEmpty();
        asAlias.StatusCode.Should().Be(HttpStatusCode.OK);
        asName.StatusCode.Should().Be(HttpStatusCode.Created, await asName.Content.ReadAsStringAsync());
        (await StoredDeletedFlagsAsync(first.Id)).Should().Equal(true);
    }

    // BR4 (design pass): the set is replaced by a diff, so an alias kept keeps its row; a kept alias typed with
    // another case or accent shows as typed now.
    [Fact]
    public async Task Update_ReplacesTheSetByDiff_KeepingTheRowOfAnAliasThatStays()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Portugues {marker}", $"Gramática {marker}");
        var before = await QueryAsync(context => context.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM catalog.subject_aliases
                WHERE subject_id = {subject.Id} AND normalized_name = {$"PORTUGUES {marker}".ToUpperInvariant()}
                """)
            .SingleAsync());

        var response = await PutAsync(admin, subject.Id, subject.Name, [$"PORTUGUÊS {marker}", $"Crase {marker}"]);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!;
        body.Aliases.Should().Equal($"Crase {marker}", $"PORTUGUÊS {marker}");
        var after = await QueryAsync(context => context.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM catalog.subject_aliases
                WHERE subject_id = {subject.Id} AND is_deleted = false AND normalized_name = {$"PORTUGUES {marker}".ToUpperInvariant()}
                """)
            .SingleAsync());
        after.Should().Be(before, "the alias that stays keeps its row");
    }

    // AC13: a null list on an update leaves the stored aliases unchanged; an empty list clears them.
    [Fact]
    public async Task Update_WithAliasesNull_LeavesTheSetUnchanged()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}");

        var response = await PutAsync(admin, subject.Id, $"Língua Portuguesa II {marker}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Aliases
            .Should().Equal($"Português {marker}");
        (await FindAsync(admin, subject.Id)).Aliases.Should().Equal($"Português {marker}");
    }

    // AC10: deleting a subject with aliases and no topics removes its aliases with it.
    [Fact]
    public async Task Delete_SubjectWithAliases_RemovesItsAliasesWithIt()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(admin, $"Língua Portuguesa {marker}", $"Português {marker}", $"Gramática {marker}");

        var response = await admin.DeleteAsync($"{Subjects}/{subject.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredDeletedFlagsAsync(subject.Id)).Should().Equal(true, true);
    }

    // AC11: the list finds a subject by an alias, and shows it once even when several aliases match.
    [Fact]
    public async Task List_SearchFindsASubjectByItsAlias_OnceAndWithItsOwnName()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var subject = await CreateAsync(
            admin, $"Direito Administrativo {marker}", $"Jurídico {marker}", $"Jurídica {marker}");

        var page = await ListAsync(admin, $"search=JURIDIC&pageSize=100");

        page.Items.Where(item => item.Id == subject.Id).Should().ContainSingle()
            .Which.Name.Should().Be($"Direito Administrativo {marker}");
    }

    // AC15 (API side): without the permission a student cannot write aliases.
    [Fact]
    public async Task Save_WithAliases_AsAStudent_IsForbidden()
    {
        var student = await StudentAsync();
        var request = new SaveSubjectRequest("Português", null, ["Língua"]);

        var create = await student.PostAsJsonAsync(Subjects, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Subjects}/{Guid.CreateVersion7()}", request, AppJson.Options);

        foreach (var response in new[] { create, update })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.Forbidden);
        }
    }
}
