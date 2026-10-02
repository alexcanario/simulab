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
/// F-42 AC8 (BR6): the migration replaces the free text of a State exam by the state's acronym when the text
/// names a state without doubt, leaves anything else as it was, and touches no Municipal or National row, in a
/// real PostgreSQL catalog.
/// </summary>
public sealed class NormalizeExamStatesMigrationTests
{
    private const string PreviousMigration = "20261001211819_HideIssuingAuthorityAcronym";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_ReplacesRecognizedStatesByTheirAcronym_AndLeavesTheRestAlone()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(NormalizeExamStatesMigrationTests));
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
        var authority = Guid.CreateVersion7();
        await ExecuteAsync(
            connection,
            $"""
            INSERT INTO catalog.issuing_authorities (id, name, normalized_name, created_at, is_deleted)
            VALUES ('{authority}', 'Governo', 'GOVERNO', now(), false)
            """);

        var withoutAccent = await InsertExamAsync(connection, authority, "Exam 1", "State", "Sao Paulo");
        var lowerCase = await InsertExamAsync(connection, authority, "Exam 2", "State", "são paulo");
        var acronym = await InsertExamAsync(connection, authority, "Exam 3", "State", "SP");
        var nameAndAcronym = await InsertExamAsync(connection, authority, "Exam 4", "State", "São Paulo (SP)");
        var withSlash = await InsertExamAsync(connection, authority, "Exam 5", "State", "Ceará/CE");
        var withDash = await InsertExamAsync(connection, authority, "Exam 6", "State", "  mato   grosso do sul - ms ");
        var unknown = await InsertExamAsync(connection, authority, "Exam 7", "State", "Sampa");
        var municipal = await InsertExamAsync(connection, authority, "Exam 8", "Municipal", "São Paulo");
        var national = await InsertExamAsync(connection, authority, "Exam 9", "National", null);

        await migrator.MigrateAsync();

        (await RowAsync(connection, withoutAccent)).Should().Be(("SP", "SAO PAULO SP"));
        (await RowAsync(connection, lowerCase)).Should().Be(("SP", "SAO PAULO SP"));
        (await RowAsync(connection, acronym)).Should().Be(("SP", "SAO PAULO SP"));
        (await RowAsync(connection, nameAndAcronym)).Should().Be(("SP", "SAO PAULO SP"));
        (await RowAsync(connection, withSlash)).Should().Be(("CE", "CEARA CE"));
        (await RowAsync(connection, withDash)).Should().Be(("MS", "MATO GROSSO DO SUL MS"));
        (await RowAsync(connection, unknown)).Should().Be(("Sampa", "SAMPA"), "a text no state matches stays as it was (BR6)");
        (await RowAsync(connection, municipal)).Should().Be(("São Paulo", "SAO PAULO"), "a Municipal row is never touched");
        (await RowAsync(connection, national)).Should().Be((null, string.Empty), "a National row is never touched");
    }

    private static async Task<Guid> InsertExamAsync(NpgsqlConnection connection, Guid authority, string name, string scope, string? detail)
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO catalog.exams
                (id, issuing_authority_id, name, assessment_type, scope, scope_detail, content_language, normalized_name,
                 normalized_scope_detail, created_at, is_deleted)
            VALUES (@id, @authority, @name, 'PublicServiceExam', @scope, CAST(@detail AS text), 'pt-BR', upper(@name),
                    upper(translate(coalesce(btrim(CAST(@detail AS text)), ''), 'ãáâéêíóôúçÃÁÂÉÊÍÓÔÚÇ', 'aaaeeiooucAAAEEIOOUC')), now(), false)
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("authority", authority);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("scope", scope);
        command.Parameters.AddWithValue("detail", (object?)detail ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();

        return id;
    }

    private static async Task<(string? ScopeDetail, string NormalizedScopeDetail)> RowAsync(NpgsqlConnection connection, Guid id)
    {
        await using var command = new NpgsqlCommand(
            "SELECT scope_detail, normalized_scope_detail FROM catalog.exams WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        return (reader.IsDBNull(0) ? null : reader.GetString(0), reader.GetString(1));
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
