using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.ExamEngine.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-167 AC12 (BR6): the migration adds the index that finds every attempt of a student, in a real PostgreSQL that
/// already holds attempts; a second run changes nothing, and going back drops only that index.
/// </summary>
public sealed class AddAttemptsStudentIndexMigrationTests
{
    private const string PreviousMigration = "20261026163439_InitialExamEngine";
    private const string Schema = ExamEngineModuleDbContext.SchemaName;
    private const string Index = "ix_attempts_student";

    private static async Task<List<string>> ValuesAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture)!);
        }

        return values;
    }

    private static Task<List<string>> IndexesAsync(NpgsqlConnection connection) =>
        ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' ORDER BY indexname");

    [Fact]
    public async Task Up_AddsTheStudentIndex_KeepsTheRows_SecondRunChangesNothing_AndDownDropsOnlyIt()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddAttemptsStudentIndexMigrationTests));
        await using var context = new ExamEngineModuleDbContext(
            new DbContextOptionsBuilder<ExamEngineModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var insert = new NpgsqlCommand(
            $"""
            INSERT INTO {Schema}.attempts (id, student_id, exam_edition_id, status, started_at, created_at, is_deleted)
            VALUES (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'InProgress', now(), now(), false),
                   (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'InProgress', now(), now(), false)
            """, connection))
        {
            await insert.ExecuteNonQueryAsync();
        }

        var before = await IndexesAsync(connection);

        await migrator.MigrateAsync();
        await migrator.MigrateAsync();

        var after = await IndexesAsync(connection);
        after.Should().BeEquivalentTo([.. before, Index], "the migration is additive: one index and nothing else");
        (await ValuesAsync(connection, $"SELECT indexdef FROM pg_indexes WHERE schemaname = '{Schema}' AND indexname = '{Index}'"))
            .Single().Should().Be($"CREATE INDEX {Index} ON {Schema}.attempts USING btree (student_id)");
        (await ValuesAsync(connection, $"SELECT count(*) FROM {Schema}.attempts")).Single().Should().Be("2");

        await migrator.MigrateAsync(PreviousMigration);

        (await IndexesAsync(connection)).Should().Equal(before, "the way down drops only the new index");
        (await ValuesAsync(connection, $"SELECT count(*) FROM {Schema}.attempts")).Single().Should().Be("2");
    }
}
