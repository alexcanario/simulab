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
/// F-36 AC4 (BR4): the migration adds <c>exams.normalized_scope_detail</c> and fills it for the rows that were
/// there before, in a real PostgreSQL catalog: accents and case gone, empty when there was no detail.
/// </summary>
public sealed class AddExamNormalizedScopeDetailMigrationTests
{
    private const string PreviousMigration = "20260929095356_FixEnumColumnDescriptions";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_FillsTheNormalizedScopeDetailOfExistingRows()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddExamNormalizedScopeDetailMigrationTests));
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
            INSERT INTO catalog.issuing_authorities (id, name, acronym, normalized_name, normalized_acronym, created_at, is_deleted)
            VALUES ('{authority}', 'Prefeitura', 'PREF', 'PREFEITURA', 'PREF', now(), false)
            """);

        var goiania = await InsertExamAsync(connection, authority, "Guarda de Goiânia", "Municipal", "Goiânia");
        var accents = await InsertExamAsync(connection, authority, "Guarda do Pará", "State", "  São João d'Aliança, Ceará – Açaí Ñandú ÁÉÍÓÚ  ");
        var national = await InsertExamAsync(connection, authority, "Exame nacional", "National", null);

        await migrator.MigrateAsync();

        (await NormalizedAsync(connection, goiania)).Should().Be("GOIANIA");
        (await NormalizedAsync(connection, accents)).Should().Be("SAO JOAO D'ALIANCA, CEARA – ACAI NANDU AEIOU");
        (await NormalizedAsync(connection, national)).Should().BeEmpty();
    }

    private static async Task<Guid> InsertExamAsync(NpgsqlConnection connection, Guid authority, string name, string scope, string? detail)
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO catalog.exams
                (id, issuing_authority_id, name, assessment_type, scope, scope_detail, content_language, normalized_name, created_at, is_deleted)
            VALUES (@id, @authority, @name, 'PublicServiceExam', @scope, @detail, 'pt-BR', upper(@name), now(), false)
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

    private static async Task<string?> NormalizedAsync(NpgsqlConnection connection, Guid id)
    {
        await using var command = new NpgsqlCommand("SELECT normalized_scope_detail FROM catalog.exams WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        return (await command.ExecuteScalarAsync())?.ToString();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
