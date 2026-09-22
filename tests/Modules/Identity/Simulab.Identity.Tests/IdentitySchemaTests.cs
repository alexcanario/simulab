using Npgsql;
using Simulab.Identity.Contracts;
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

    /// <summary>
    /// B-13 AC1: every column that serves one user or role references it, with the agreed delete rule (D2).
    /// The list is exact, so a foreign key on an audit column or on <c>role_changes</c> (D1) fails it too.
    /// </summary>
    [Fact]
    public async Task Migration_CreatesForeignKeyForEveryUserAndRoleColumn()
    {
        await using var connection = await OpenAsync();

        var keys = await ReadNamesAsync(connection,
            $"""
            SELECT dependent.relname || '.' || column_name.attname || ' -> ' || principal.relname || ' ' || constraint_row.confdeltype::text
            FROM pg_constraint constraint_row
            JOIN pg_class dependent ON dependent.oid = constraint_row.conrelid
            JOIN pg_namespace schema_row ON schema_row.oid = dependent.relnamespace
            JOIN pg_class principal ON principal.oid = constraint_row.confrelid
            JOIN pg_attribute column_name ON column_name.attrelid = constraint_row.conrelid AND column_name.attnum = constraint_row.conkey[1]
            WHERE constraint_row.contype = 'f'
              AND schema_row.nspname = '{IdentityModuleDbContext.SchemaName}'
              AND principal.relname IN ('users', 'roles')
            """);

        // confdeltype: c = cascade, r = restrict.
        keys.Should().BeEquivalentTo(
        [
            "user_roles.user_id -> users c",
            "user_roles.role_id -> roles c",
            "user_claims.user_id -> users c",
            "user_logins.user_id -> users c",
            "user_tokens.user_id -> users c",
            "email_verification_tokens.user_id -> users c",
            "password_reset_tokens.user_id -> users c",
            "role_claims.role_id -> roles c",
            "consent_records.user_id -> users r",
            "role_permissions.role_id -> roles c",
        ]);
    }

    /// <summary>B-13 AC2: the database itself refuses a role assignment for a user that does not exist.</summary>
    [Fact]
    public async Task UserRole_WithUnknownUser_IsRefused()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            $"""
            INSERT INTO {IdentityModuleDbContext.SchemaName}.user_roles (user_id, role_id)
            SELECT @userId, id FROM {IdentityModuleDbContext.SchemaName}.roles LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("userId", Guid.CreateVersion7());

        var insert = async () => await command.ExecuteNonQueryAsync();

        await insert.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.ForeignKeyViolation);
    }

    /// <summary>B-7 AC4: the columns are as wide as the limits the Api checks, read from the migrated database.</summary>
    [Theory]
    [InlineData("full_name", AccountLimits.FullNameMaxLength)]
    [InlineData("email", AccountLimits.EmailMaxLength)]
    [InlineData("normalized_email", AccountLimits.EmailMaxLength)]
    [InlineData("user_name", AccountLimits.EmailMaxLength)]
    [InlineData("normalized_user_name", AccountLimits.EmailMaxLength)]
    public async Task UserColumns_AreAsWideAsTheAccountLimits(string column, int length)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT character_maximum_length FROM information_schema.columns WHERE table_schema = @schema AND table_name = 'users' AND column_name = @column",
            connection);
        command.Parameters.AddWithValue("schema", IdentityModuleDbContext.SchemaName);
        command.Parameters.AddWithValue("column", column);

        (await command.ExecuteScalarAsync()).Should().Be(length);
    }
}
