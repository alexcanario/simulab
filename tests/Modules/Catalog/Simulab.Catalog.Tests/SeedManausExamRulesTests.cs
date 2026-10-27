using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-168 AC13 and BR9: the <c>SeedManausExamRules</c> data migration writes the Manaus 2026 rules of the notice
/// (Quadro I, items 5.1.4, 5.2.7, 5.2.9 and 6.1.2), each value only where it is still empty. Checked in a real
/// PostgreSQL database, one per test, because several tests write values before the seed.
/// </summary>
public sealed class SeedManausExamRulesTests
{
    private const string SchemaMigration = "AddExamEditionRules";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    // The Manaus 2026 edition of the F-37 seed.
    private static readonly Guid Manaus = Guid.Parse("0198f370-0004-7000-8000-000000000002");

    // Quadro I of the notice, written here on its own so a slip in the migration shows up.
    private static readonly Dictionary<string, decimal> Points = new()
    {
        ["Língua Portuguesa"] = 1.50m,
        ["Informática"] = 1.50m,
        ["Geografia e História de Manaus"] = 1.50m,
        ["Ética e Direitos Humanos"] = 1.50m,
        ["Direito Constitucional"] = 2.00m,
        ["Direito Penal"] = 1.50m,
        ["Direito Processual Penal"] = 1.50m,
        ["Legislação de trânsito"] = 1.50m,
        ["Legislação específica"] = 2.00m,
    };

    [Fact]
    public async Task Migrate_EmptyDatabase_ManausHasTheRulesOfItsNotice()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        (await database.ValueAsync($"SELECT duration_minutes FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(300m);
        (await database.ValueAsync($"SELECT objective_cut_off_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(60.00m);
        (await database.ValueAsync($"SELECT essay_max_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(20.00m);
        (await database.ValueAsync($"SELECT essay_cut_off_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(10.00m);
        database.Context.Database.HasPendingModelChanges().Should().BeFalse("a data migration changes no model");
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EachManausSubjectHasItsPointsAndTheTotalIs100()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var stored = await database.PointsAsync(Manaus);

        stored.Should().BeEquivalentTo(Points);
        (await database.ValueAsync(
            $"SELECT sum(question_count * points_per_question) FROM {Schema}.notice_subjects WHERE exam_edition_id = '{Manaus}' AND NOT is_deleted"))
            .Should().Be(100.00m);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_NoOtherEditionGetsARuleOrPoints()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        (await database.ValueAsync(
            $"SELECT count(*) FROM {Schema}.exam_editions WHERE id <> '{Manaus}' AND (duration_minutes IS NOT NULL OR objective_cut_off_score IS NOT NULL OR essay_max_score IS NOT NULL OR essay_cut_off_score IS NOT NULL)"))
            .Should().Be(0m);
        (await database.ValueAsync(
            $"SELECT count(*) FROM {Schema}.notice_subjects WHERE exam_edition_id <> '{Manaus}' AND points_per_question IS NOT NULL"))
            .Should().Be(0m);
    }

    // BR9: a value an admin already typed is kept; only the empty ones are filled.
    [Fact]
    public async Task Migrate_AValueAnAdminTyped_IsKept_AndTheEmptyOnesAreFilled()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(SchemaMigration);
        await database.ExecuteAsync(
            $"UPDATE {Schema}.exam_editions SET duration_minutes = 240, objective_cut_off_score = 50 WHERE id = '{Manaus}'");
        await database.ExecuteAsync(
            $"UPDATE {Schema}.notice_subjects SET points_per_question = 3 WHERE exam_edition_id = '{Manaus}' AND label = 'Informática'");

        await database.MigrateAsync();

        (await database.ValueAsync($"SELECT duration_minutes FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(240m);
        (await database.ValueAsync($"SELECT objective_cut_off_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(50.00m);
        (await database.ValueAsync($"SELECT essay_max_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(20.00m);
        (await database.ValueAsync($"SELECT essay_cut_off_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(10.00m);
        var stored = await database.PointsAsync(Manaus);
        stored["Informática"].Should().Be(3.00m);
        stored["Língua Portuguesa"].Should().Be(1.50m);
    }

    // Architect 3: an essay maximum an admin set below 10 keeps the essay cut-off empty, and the migration succeeds.
    [Fact]
    public async Task Migrate_AnEssayMaximumBelowTheSeededCutOff_LeavesTheCutOffEmptyAndSucceeds()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(SchemaMigration);
        await database.ExecuteAsync($"UPDATE {Schema}.exam_editions SET essay_max_score = 8 WHERE id = '{Manaus}'");

        await database.MigrateAsync();

        (await database.ValueAsync($"SELECT essay_max_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().Be(8.00m);
        (await database.ValueAsync($"SELECT essay_cut_off_score FROM {Schema}.exam_editions WHERE id = '{Manaus}'")).Should().BeNull();
    }

    // Architect 4: a subject an admin added to Manaus is not one of the nine of the notice, so it gets no points.
    [Fact]
    public async Task Migrate_ASubjectAnAdminAddedToManaus_GetsNoPoints()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(SchemaMigration);
        await database.ExecuteAsync(
            $"INSERT INTO {Schema}.notice_subjects (id, exam_edition_id, normalized_group, label, normalized_label, question_count, display_order, tenant_id, created_at, is_deleted) " +
            $"VALUES ('{Guid.CreateVersion7()}', '{Manaus}', '', 'Matéria do administrador', 'MATERIA DO ADMINISTRADOR', 5, 99, NULL, now(), false)");

        await database.MigrateAsync();

        (await database.PointsAsync(Manaus)).Should().Contain(new KeyValuePair<string, decimal?>("Matéria do administrador", null));
    }

    // A database check holds the essay pair together: a cut-off with no maximum is refused by the table too.
    [Fact]
    public async Task Table_AnEssayCutOffWithoutAMaximum_IsRefused()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var write = () => database.ExecuteAsync(
            $"UPDATE {Schema}.exam_editions SET essay_max_score = NULL, essay_cut_off_score = 10 WHERE id = '{Manaus}'");

        (await write.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    private static async Task<Database> DatabaseAsync()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedManausExamRulesTests) + Guid.CreateVersion7().ToString("N"));
        var context = new CatalogModuleDbContext(
            new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());

        return new Database(context, connectionString);
    }

    private sealed class Database(CatalogModuleDbContext context, string connectionString) : IAsyncDisposable
    {
        public CatalogModuleDbContext Context { get; } = context;

        public Task MigrateAsync(string? target = null) => Context.GetService<IMigrator>().MigrateAsync(target);

        public async Task<int> ExecuteAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<decimal?> ValueAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            var value = await command.ExecuteScalarAsync();

            return value is null or DBNull ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public async Task<Dictionary<string, decimal?>> PointsAsync(Guid edition)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"SELECT label, points_per_question FROM {Schema}.notice_subjects WHERE exam_edition_id = '{edition}' AND NOT is_deleted",
                connection);
            await using var reader = await command.ExecuteReaderAsync();
            var points = new Dictionary<string, decimal?>();
            while (await reader.ReadAsync())
            {
                points[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetDecimal(1);
            }

            return points;
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
