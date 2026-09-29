using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// B-20 AC2 and AC4: the migration rewrites the comments of the three Identity enum columns in a real
/// PostgreSQL catalog and touches nothing else about them.
/// </summary>
public sealed class FixEnumColumnDescriptionsMigrationTests
{
    private const string PreviousMigration = "20260925171652_AddColumnDescriptions";
    private const string Schema = IdentityModuleDbContext.SchemaName;

    public static TheoryData<string, string, string> Columns => new()
    {
        { "users", "status", "Where the account stands: Pending, Active or Erased." },
        { "role_changes", "action", "What was done: RoleCreated, RoleUpdated, RoleDeleted or UserRolesChanged." },
        { "account_events", "reason", "Why it failed, such as WrongPassword or LockedOut. Null when nothing failed." },
    };

    [Theory]
    [MemberData(nameof(Columns))]
    public async Task Up_RewritesTheComment_AndNothingElseAboutTheColumn(string table, string column, string expected)
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync($"{nameof(FixEnumColumnDescriptionsMigrationTests)}_{table}");
        await using var context = new IdentityModuleDbContext(
            new DbContextOptionsBuilder<IdentityModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var shapeBefore = await ScalarAsync(connection, Shape, table, column);
        var commentBefore = await ScalarAsync(connection, Comment, table, column);

        await migrator.MigrateAsync();

        (await ScalarAsync(connection, Comment, table, column)).Should().Be(expected).And.NotBe(commentBefore);
        (await ScalarAsync(connection, Shape, table, column)).Should().Be(shapeBefore);
    }

    private const string Shape =
        """
        SELECT format_type(a.atttypid, a.atttypmod) || ' ' || a.attnotnull
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'identity' AND c.relname = @table AND a.attname = @column
        """;

    private const string Comment =
        """
        SELECT col_description(c.oid, a.attnum)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'identity' AND c.relname = @table AND a.attname = @column
        """;

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql, string table, string column)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
