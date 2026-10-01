using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-37: the municipal guard catalog the <c>SeedMunicipalGuardCatalog</c> data migration writes, checked in a
/// real PostgreSQL database that ran every catalog migration. Each test gets its own database, because the
/// soft-delete test changes rows.
/// </summary>
public sealed class SeedMunicipalGuardCatalogTests
{
    private const string Schema = CatalogModuleDbContext.SchemaName;
    private const int MaxNoticeYear = 2027;

    private static readonly Guid CuritibaExam = Guid.Parse("0198f370-0003-7000-8000-000000000001");
    private static readonly Guid RecifeExam = Guid.Parse("0198f370-0003-7000-8000-000000000004");
    private static readonly Guid GoianiaExam = Guid.Parse("0198f370-0003-7000-8000-000000000005");

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsTheCatalogWithNoTenant()
    {
        await using var seeded = await SeededAsync();

        (await seeded.Context.Set<Organizer>().IgnoreQueryFilters().CountAsync()).Should().Be(7);
        (await seeded.Context.Set<IssuingAuthority>().IgnoreQueryFilters().CountAsync()).Should().Be(6);
        (await seeded.Context.Set<Exam>().IgnoreQueryFilters().CountAsync()).Should().Be(6);
        (await seeded.Context.Set<ExamEdition>().IgnoreQueryFilters().CountAsync()).Should().Be(4);

        var tenants = await ScalarAsync(
            seeded,
            """
            SELECT (SELECT count(*) FROM catalog.organizers WHERE tenant_id IS NOT NULL)
                 + (SELECT count(*) FROM catalog.issuing_authorities WHERE tenant_id IS NOT NULL)
                 + (SELECT count(*) FROM catalog.exams WHERE tenant_id IS NOT NULL)
                 + (SELECT count(*) FROM catalog.exam_editions WHERE tenant_id IS NOT NULL)
            """);
        tenants.Should().Be(0);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_ListsTheSevenBoardsOnceEach()
    {
        await using var seeded = await SeededAsync();

        var boards = await seeded.Context.Set<Organizer>().AsNoTracking().ToListAsync();

        boards.Select(board => board.Name).Should().BeEquivalentTo(
            "Instituto AOCP",
            "Instituto Consulplan",
            "Fundação Getulio Vargas (FGV)",
            "Copeve/Ufal",
            "Vunesp",
            "FCC",
            "Cebraspe");
        boards.Should().OnlyContain(board => board.Kind == OrganizerKind.ExamBoard);
        boards.Should().OnlyContain(board => board.Acronym.All(letter => !char.IsLower(letter)));
        boards.Count(board => board.Name.Contains("AOCP")).Should().Be(1);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_HasTheFixedIdsOfTheSeed()
    {
        await using var seeded = await SeededAsync();

        var curitiba = await seeded.Context.Set<Exam>().AsNoTracking().SingleAsync(exam => exam.Id == CuritibaExam);

        curitiba.ScopeDetail.Should().Be("Curitiba");
        (await seeded.Context.Set<Organizer>().AsNoTracking().Select(board => board.Id).ToListAsync())
            .Should().OnlyHaveUniqueItems()
            .And.OnlyContain(id => id.ToString().StartsWith("0198f370-0001-7000-8000-"));
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EverySeededRowEqualsWhatTheDomainComputes()
    {
        await using var seeded = await SeededAsync();
        var context = seeded.Context;

        foreach (var stored in await context.Set<Organizer>().AsNoTracking().ToListAsync())
        {
            var rebuilt = Organizer.Create(stored.Name, stored.Acronym, stored.Kind, stored.Description, stored.Website);

            rebuilt.IsSuccess.Should().BeTrue(stored.Name);
            rebuilt.Value.Should().BeEquivalentTo(stored, options => options.ComparingByMembers<Organizer>().Including(o => o.Name)
                .Including(o => o.Acronym).Including(o => o.Kind).Including(o => o.NormalizedName).Including(o => o.NormalizedAcronym));
        }

        foreach (var stored in await context.Set<IssuingAuthority>().AsNoTracking().ToListAsync())
        {
            var rebuilt = IssuingAuthority.Create(stored.Name, stored.Description, stored.Website);

            rebuilt.IsSuccess.Should().BeTrue(stored.Name);
            rebuilt.Value.Should().BeEquivalentTo(stored, options => options.Including(a => a.Name).Including(a => a.NormalizedName));
        }

        foreach (var stored in await context.Set<Exam>().AsNoTracking().ToListAsync())
        {
            var rebuilt = Exam.Create(stored.IssuingAuthorityId, stored.Name, stored.AssessmentType, stored.Scope, stored.ScopeDetail, stored.ContentLanguage);

            rebuilt.IsSuccess.Should().BeTrue(stored.Name);
            rebuilt.Value.Should().BeEquivalentTo(stored, options => options.Including(e => e.Name).Including(e => e.ScopeDetail)
                .Including(e => e.ContentLanguage).Including(e => e.NormalizedName).Including(e => e.NormalizedScopeDetail));
        }

        foreach (var stored in await context.Set<ExamEdition>().AsNoTracking().ToListAsync())
        {
            var rebuilt = ExamEdition.Create(
                stored.ExamId,
                stored.OrganizerId,
                stored.NoticeYear,
                stored.Position,
                stored.NoticeReference,
                stored.NoticeUrl,
                stored.AppliedOn,
                stored.Status,
                MaxNoticeYear);

            rebuilt.IsSuccess.Should().BeTrue(stored.Position);
            rebuilt.Value.Should().BeEquivalentTo(stored, options => options.Including(e => e.Position)
                .Including(e => e.NormalizedPosition).Including(e => e.NoticeReference).Including(e => e.NoticeUrl));
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_RecifeAndGoianiaHaveNoEditionAndRioIsAbsent()
    {
        await using var seeded = await SeededAsync();
        var context = seeded.Context;

        (await context.Set<Exam>().AsNoTracking().Where(exam => exam.Id == RecifeExam || exam.Id == GoianiaExam).CountAsync()).Should().Be(2);
        (await context.Set<ExamEdition>().AsNoTracking().CountAsync(edition => edition.ExamId == RecifeExam || edition.ExamId == GoianiaExam))
            .Should().Be(0);
        (await context.Set<Exam>().AsNoTracking().CountAsync(exam => exam.NormalizedScopeDetail.Contains("RIO"))).Should().Be(0);
        (await context.Set<IssuingAuthority>().AsNoTracking().CountAsync(authority => authority.NormalizedName.Contains("RIO DE JANEIRO")))
            .Should().Be(0);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EveryEditionPointsToAnExistingExamAndBoardInsideTheYearRange()
    {
        await using var seeded = await SeededAsync();
        var context = seeded.Context;
        var editions = await context.Set<ExamEdition>().AsNoTracking().ToListAsync();
        var examIds = await context.Set<Exam>().AsNoTracking().Select(exam => exam.Id).ToListAsync();
        var boardIds = await context.Set<Organizer>().AsNoTracking().Select(board => board.Id).ToListAsync();

        editions.Should().OnlyContain(edition => examIds.Contains(edition.ExamId) && boardIds.Contains(edition.OrganizerId));
        editions.Should().OnlyContain(edition => edition.NoticeYear >= CatalogLimits.ExamEditionNoticeYearMin && edition.NoticeYear <= MaxNoticeYear);
        editions.Select(edition => (edition.ExamId, edition.NoticeYear, edition.NormalizedPosition, edition.OrganizerId))
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_AnEditionIsPublishedExactlyWhenItHasAReferenceAndAnAbsoluteLink()
    {
        await using var seeded = await SeededAsync();

        var editions = await seeded.Context.Set<ExamEdition>().AsNoTracking().ToListAsync();

        foreach (var edition in editions)
        {
            var confirmed = !string.IsNullOrWhiteSpace(edition.NoticeReference)
                && Uri.TryCreate(edition.NoticeUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            edition.Status.Should().Be(confirmed ? ExamEditionStatus.Published : ExamEditionStatus.Draft, edition.Position);
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_NoEditionCarriesAnApplicationDateNoSourceConfirmed()
    {
        await using var seeded = await SeededAsync();

        (await seeded.Context.Set<ExamEdition>().AsNoTracking().CountAsync(edition => edition.AppliedOn != null)).Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_OnTheSeededDatabase_ReturnsTheExamsWithAPublishedEditionAndNoOther()
    {
        await using var seeded = await SeededAsync();

        var page = await new PublishedExamQueries(seeded.Context).ListAsync(new PublishedExamListQuery(), CancellationToken.None);

        page.Total.Should().Be(4);
        page.Items.Select(item => item.Id).Should().NotContain([RecifeExam, GoianiaExam]);
        page.Items.Select(item => item.ScopeDetail).Should().BeEquivalentTo("Curitiba", "Manaus", "Salvador", "Maceió");
    }

    [Fact]
    public async Task Migrate_AgainAfterAnAdminDeletedASeededRow_KeepsItDeletedAndAddsNoDuplicate()
    {
        await using var seeded = await SeededAsync();
        await using var connection = new NpgsqlConnection(seeded.ConnectionString);
        await connection.OpenAsync();
        await using (var delete = new NpgsqlCommand(
            "UPDATE catalog.exams SET is_deleted = true, deleted_at = now() WHERE id = @id", connection))
        {
            delete.Parameters.AddWithValue("id", RecifeExam);
            await delete.ExecuteNonQueryAsync();
        }

        await seeded.Context.GetService<IMigrator>().MigrateAsync();

        (await ScalarAsync(seeded, "SELECT count(*) FROM catalog.exams")).Should().Be(6);
        (await ScalarAsync(seeded, $"SELECT count(*) FROM catalog.exams WHERE id = '{RecifeExam}' AND is_deleted")).Should().Be(1);
    }

    private static async Task<SeededCatalog> SeededAsync()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedMunicipalGuardCatalogTests) + Guid.CreateVersion7().ToString("N"));
        var context = new CatalogModuleDbContext(
            new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        await context.GetService<IMigrator>().MigrateAsync();

        return new SeededCatalog(context, connectionString);
    }

    private static async Task<long> ScalarAsync(SeededCatalog seeded, string sql)
    {
        await using var connection = new NpgsqlConnection(seeded.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private sealed class SeededCatalog(CatalogModuleDbContext context, string connectionString) : IAsyncDisposable
    {
        public CatalogModuleDbContext Context { get; } = context;

        public string ConnectionString { get; } = connectionString;

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
