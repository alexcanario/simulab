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
/// F-75: applied to a database that has the previous migration, the new one creates the mappings table, its
/// four indexes and its check constraint in a real PostgreSQL, and going back drops the table and leaves the
/// notice subjects alone.
/// </summary>
public sealed class AddNoticeSubjectMappingsMigrationTests
{
    private const string PreviousMigration = "20261005135846_SeedGuardTaxonomy";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_CreatesTheTableItsIndexesAndItsCheck_AndDownDropsThem()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddNoticeSubjectMappingsMigrationTests));
        await using var context = new CatalogModuleDbContext(
            new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        (await TablesAsync(connection)).Should().NotContain("notice_subject_mappings");

        await migrator.MigrateAsync();

        (await TablesAsync(connection)).Should().Contain("notice_subject_mappings");
        (await ColumnAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'notice_subject_mappings'"))
            .Should().BeEquivalentTo(
                "pk_notice_subject_mappings",
                "ix_notice_subject_mappings_notice_subject",
                "ix_notice_subject_mappings_subject",
                "ix_notice_subject_mappings_topic",
                "ux_notice_subject_mappings_tenant_notice_subject_target");
        (await ColumnAsync(connection, "SELECT conname FROM pg_constraint WHERE contype = 'c' AND conname = 'ck_notice_subject_mappings_one_target'"))
            .Should().ContainSingle();

        await migrator.MigrateAsync(PreviousMigration);

        var tables = await TablesAsync(connection);
        tables.Should().NotContain("notice_subject_mappings");
        tables.Should().Contain("notice_subjects");
    }

    private static Task<List<string>> TablesAsync(NpgsqlConnection connection) =>
        ColumnAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}'");

    private static async Task<List<string>> ColumnAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
