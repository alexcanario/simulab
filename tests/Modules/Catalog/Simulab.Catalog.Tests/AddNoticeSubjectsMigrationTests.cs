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
/// F-74: applied to a database that has the previous migration, the new one creates the notice subjects table
/// and its two indexes in a real PostgreSQL, and going back drops the table and leaves the editions alone.
/// </summary>
public sealed class AddNoticeSubjectsMigrationTests
{
    private const string PreviousMigration = "20261004180816_AddSubjectTaxonomy";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_CreatesTheTableAndItsIndexes_AndDownDropsThem()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddNoticeSubjectsMigrationTests));
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
        (await TablesAsync(connection)).Should().NotContain("notice_subjects");

        await migrator.MigrateAsync();

        (await TablesAsync(connection)).Should().Contain("notice_subjects");
        (await IndexesAsync(connection)).Should().BeEquivalentTo(
            "pk_notice_subjects",
            "ix_notice_subjects_edition_order",
            "ux_notice_subjects_tenant_edition_group_label");

        await migrator.MigrateAsync(PreviousMigration);

        var tables = await TablesAsync(connection);
        tables.Should().NotContain("notice_subjects");
        tables.Should().Contain("exam_editions");
    }

    private static Task<List<string>> TablesAsync(NpgsqlConnection connection) =>
        ColumnAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}'");

    private static Task<List<string>> IndexesAsync(NpgsqlConnection connection) =>
        ColumnAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'notice_subjects'");

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
