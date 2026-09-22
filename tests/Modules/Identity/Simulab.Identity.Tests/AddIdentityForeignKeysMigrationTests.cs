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
/// B-13 AC3 (D3): the migration that adds the foreign keys removes the rows that point to nothing first,
/// and keeps every row that points to a user or role that exists, soft deleted or not.
/// </summary>
public sealed class AddIdentityForeignKeysMigrationTests
{
    private const string PreviousMigration = "20260921161225_AddRoleChanges";
    private const string Schema = IdentityModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_RemovesOrphansAndKeepsTheRest()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddIdentityForeignKeysMigrationTests));
        await using var context = CreateContext(connectionString);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var userId = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        var missingUserId = Guid.CreateVersion7();
        var missingRoleId = Guid.CreateVersion7();

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection,
                $"""
                INSERT INTO {Schema}.users
                    (id, tenant_id, email, normalized_email, user_name, normalized_user_name, email_confirmed,
                     password_hash, security_stamp, concurrency_stamp, phone_number_confirmed, two_factor_enabled,
                     lockout_enabled, access_failed_count, status, preferred_language, is_adult_declared,
                     created_at, is_deleted)
                VALUES
                    (@userId, NULL, 'kept@example.com', 'KEPT@EXAMPLE.COM', 'kept@example.com', 'KEPT@EXAMPLE.COM', true,
                     'x', 'x', 'x', false, false,
                     false, 0, 'Active', 'en', true,
                     now(), true);
                INSERT INTO {Schema}.roles (id, name, normalized_name, is_system, created_at, is_deleted)
                VALUES (@roleId, 'Kept', 'KEPT', false, now(), false);
                INSERT INTO {Schema}.user_tokens (user_id, login_provider, name, value)
                VALUES (@userId, 'Simulab', 'kept', 'x'), (@missingUserId, 'Simulab', 'orphan', 'x');
                INSERT INTO {Schema}.consent_records
                    (id, user_id, accepted_at, terms_version, privacy_version, locale, declares_adult, created_at, is_deleted)
                VALUES
                    (@keptConsent, @userId, now(), '1', '1', 'en', true, now(), false),
                    (@orphanConsent, @missingUserId, now(), '1', '1', 'en', true, now(), false);
                INSERT INTO {Schema}.role_claims (role_id, claim_type, claim_value)
                VALUES (@roleId, 'kept', 'x'), (@missingRoleId, 'orphan', 'x');
                """,
                ("userId", userId), ("roleId", roleId), ("missingUserId", missingUserId), ("missingRoleId", missingRoleId),
                ("keptConsent", Guid.CreateVersion7()), ("orphanConsent", Guid.CreateVersion7()));
        }

        await migrator.MigrateAsync();

        await using var check = new NpgsqlConnection(connectionString);
        await check.OpenAsync();
        (await ReadAsync(check, $"SELECT user_id FROM {Schema}.user_tokens")).Should().Equal(userId);
        (await ReadAsync(check, $"SELECT user_id FROM {Schema}.consent_records")).Should().Equal(userId);
        (await ReadAsync(check, $"SELECT role_id FROM {Schema}.role_claims")).Should().Equal(roleId);
        (await ReadAsync(check,
            $"SELECT confrelid FROM pg_constraint WHERE contype = 'f' AND conname = 'fk_user_tokens_users_user_id'"))
            .Should().ContainSingle("the foreign key was added after the clean-up");
    }

    private static IdentityModuleDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, Guid Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<object>> ReadAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var values = new List<object>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetValue(0));
        }

        return values;
    }
}
