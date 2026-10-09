using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>F-53 AC1: the migration adds the mark to a real PostgreSQL table that already holds accounts, and none is marked.</summary>
public sealed class AddMustChangePasswordMigrationTests
{
    private const string PreviousMigration = "20260929095402_FixEnumColumnDescriptions";
    private const string Schema = IdentityModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_AccountsExist_EveryAccountIsUnmarked_AndTheColumnHasItsDescription()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddMustChangePasswordMigrationTests));
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
        await InsertAccountAsync(connection, "first@exemplo.com");
        await InsertAccountAsync(connection, "second@exemplo.com");

        await migrator.MigrateAsync();

        (await ScalarAsync(connection, "SELECT count(*) FROM identity.users")).Should().Be("2", "the accounts are still there");
        (await ScalarAsync(connection, "SELECT count(*) FROM identity.users WHERE must_change_password")).Should().Be("0");
        (await ScalarAsync(connection, ColumnComment)).Should().StartWith("True when the person must pick a new password");
        (await ScalarAsync(connection, ColumnShape)).Should().Be("boolean true true", "a boolean, not null, whose default is false");
    }

    /// <summary>One row with a value for every column the table requires and does not default, whatever the table asks for later.</summary>
    private static async Task InsertAccountAsync(NpgsqlConnection connection, string email)
    {
        var required = new List<(string Name, string Type)>();
        await using (var columns = new NpgsqlCommand(
            """
            SELECT column_name, data_type FROM information_schema.columns
            WHERE table_schema = 'identity' AND table_name = 'users' AND is_nullable = 'NO' AND column_default IS NULL
            """, connection))
        await using (var reader = await columns.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                required.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        required.Should().NotBeEmpty("the users table has required columns, so this reads the real schema");

        // The user name is optional but unique with the tenant (two nulls count as equal): each row needs its own.
        foreach (var unique in new[] { "user_name", "normalized_user_name" })
        {
            if (required.All(column => column.Name != unique))
            {
                required.Add((unique, "text"));
            }
        }

        var names = string.Join(", ", required.Select(column => $"\"{column.Name}\""));
        var values = string.Join(", ", required.Select(column => column.Name switch
        {
            "email" or "normalized_email" or "user_name" or "normalized_user_name" => $"'{email}'",
            _ => column.Type switch
            {
                "uuid" => "gen_random_uuid()",
                "boolean" => "false",
                "integer" or "bigint" => "0",
                "timestamp with time zone" => "now()",
                _ => "'x'",
            },
        }));

        await using var insert = new NpgsqlCommand($"INSERT INTO identity.users ({names}) VALUES ({values})", connection);
        await insert.ExecuteNonQueryAsync();
    }

    private const string ColumnShape =
        """
        SELECT format_type(a.atttypid, a.atttypmod) || ' ' || a.attnotnull || ' ' || (pg_get_expr(d.adbin, d.adrelid) = 'false')
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        LEFT JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
        WHERE n.nspname = 'identity' AND c.relname = 'users' AND a.attname = 'must_change_password'
        """;

    private const string ColumnComment =
        """
        SELECT col_description(c.oid, a.attnum)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'identity' AND c.relname = 'users' AND a.attname = 'must_change_password'
        """;

    private static async Task<string?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
