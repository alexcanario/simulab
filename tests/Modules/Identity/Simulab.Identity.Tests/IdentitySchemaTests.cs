using Npgsql;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Persistence;

namespace Simulab.Identity.Tests;

/// <summary>AC13: the module's tables, their names and the unique index that has to treat two nulls as equal.</summary>
public sealed class IdentitySchemaTests : IdentityApiTests
{
    private async Task<NpgsqlConnection> OpenAsync()
    {
        // Touching Services starts the host, which applies the migrations this test is about.
        _ = Factory.Services;
        var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<List<string>> ReadNamesAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    [Fact]
    public async Task Migration_CreatesEveryTableInTheModuleSchema()
    {
        await using var connection = await OpenAsync();

        var tables = await ReadNamesAsync(connection,
            $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{IdentityModuleDbContext.SchemaName}'");

        tables.Should().Contain(["users", "consent_records", "email_verification_tokens", "password_reset_tokens"]);
        tables.Should().Contain(PersistenceServiceCollectionExtensions.HistoryTableName,
            "the module keeps its own migrations history in its own schema");
    }

    [Fact]
    public async Task Migration_NamesEveryColumnInSnakeCase()
    {
        await using var connection = await OpenAsync();

        var columns = await ReadNamesAsync(connection,
            $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{IdentityModuleDbContext.SchemaName}'");

        columns.Should().NotBeEmpty();
        columns.Should().OnlyContain(name => !name.Any(char.IsUpper), "snake_case has no capitals");
        columns.Should().Contain(["email_verified_at", "is_adult_declared", "preferred_language", "terms_version", "privacy_version"]);
    }

    [Fact]
    public async Task EmailIndex_TreatsTwoGlobalRowsAsADuplicate()
    {
        await using var connection = await OpenAsync();
        await InsertUserAsync(connection, "MESMO@EXEMPLO.COM");

        // Two global accounts (tenant_id null) with one address: without NULLS NOT DISTINCT both would pass.
        var second = async () => await InsertUserAsync(connection, "MESMO@EXEMPLO.COM");

        await second.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.UniqueViolation);
    }

    private static async Task InsertUserAsync(NpgsqlConnection connection, string normalizedEmail)
    {
        await using var command = new NpgsqlCommand(
            $"""
            INSERT INTO {IdentityModuleDbContext.SchemaName}.users
                (id, tenant_id, email, normalized_email, user_name, normalized_user_name, email_confirmed,
                 password_hash, security_stamp, concurrency_stamp, phone_number_confirmed, two_factor_enabled,
                 lockout_enabled, access_failed_count, status, preferred_language, is_adult_declared,
                 created_at, is_deleted)
            VALUES
                (@id, NULL, @email, @normalized, @email, @userName, false,
                 'x', 'x', 'x', false, false,
                 false, 0, 'Pending', 'en', true,
                 now(), false)
            """,
            connection);

        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("email", normalizedEmail.ToLowerInvariant());
        command.Parameters.AddWithValue("normalized", normalizedEmail);
        command.Parameters.AddWithValue("userName", normalizedEmail + Guid.CreateVersion7().ToString("N"));
        await command.ExecuteNonQueryAsync();
    }
}
