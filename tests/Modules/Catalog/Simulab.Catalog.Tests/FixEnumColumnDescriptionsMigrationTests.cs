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
/// B-20 AC2 and AC4: the migration rewrites the comment of <c>exams.scope</c> in a real PostgreSQL catalog and
/// touches nothing else about the column.
/// </summary>
public sealed class FixEnumColumnDescriptionsMigrationTests
{
    private const string PreviousMigration = "20260928215100_AddExamEditions";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    private const string ColumnShape =
        """
        SELECT format_type(a.atttypid, a.atttypmod) || ' ' || a.attnotnull
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'catalog' AND c.relname = 'exams' AND a.attname = 'scope'
        """;

    private const string ColumnComment =
        """
        SELECT col_description(c.oid, a.attnum)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'catalog' AND c.relname = 'exams' AND a.attname = 'scope'
        """;

    [Fact]
    public async Task Up_RewritesTheScopeComment_AndNothingElseAboutTheColumn()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(FixEnumColumnDescriptionsMigrationTests));
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
        var shapeBefore = await ScalarAsync(connection, ColumnShape);
        (await ScalarAsync(connection, ColumnComment)).Should().Contain("Federal");

        await migrator.MigrateAsync();

        (await ScalarAsync(connection, ColumnComment)).Should().Be("How far the exam reaches: National, State or Municipal.");
        (await ScalarAsync(connection, ColumnShape)).Should().Be(shapeBefore);
    }

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
