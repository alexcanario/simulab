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
/// F-89: applied to a database that has the previous migration, the new one adds the four notice document columns
/// and their check to the editions in a real PostgreSQL, and going back drops them and keeps the editions.
/// </summary>
public sealed class AddExamEditionNoticeDocumentMigrationTests
{
    private const string PreviousMigration = "20261019092342_SeedGuardNoticeSubjects";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    private static readonly string[] Columns =
    [
        "notice_document_blob_name",
        "notice_document_file_name",
        "notice_document_size",
        "notice_document_uploaded_at"
    ];

    [Fact]
    public async Task Up_AddsTheFourColumnsAndTheCheck_AndDownDropsThem()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddExamEditionNoticeDocumentMigrationTests));
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
        (await ColumnNamesAsync(connection)).Should().NotContain(Columns);

        await migrator.MigrateAsync();

        (await ColumnNamesAsync(connection)).Should().Contain(Columns);
        (await ConstraintsAsync(connection)).Should().Contain("ck_exam_editions_notice_document");

        await migrator.MigrateAsync(PreviousMigration);

        (await ColumnNamesAsync(connection)).Should().NotContain(Columns);
        (await ConstraintsAsync(connection)).Should().NotContain("ck_exam_editions_notice_document");
    }

    private static Task<List<string>> ColumnNamesAsync(NpgsqlConnection connection) =>
        ColumnAsync(connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'exam_editions'");

    private static Task<List<string>> ConstraintsAsync(NpgsqlConnection connection) =>
        ColumnAsync(connection, $"SELECT conname FROM pg_constraint WHERE conrelid = '{Schema}.exam_editions'::regclass");

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
