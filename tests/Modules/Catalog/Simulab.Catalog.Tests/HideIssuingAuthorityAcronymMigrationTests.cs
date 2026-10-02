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
/// F-44 AC4 (BR4): the migration makes the acronym columns optional and drops the unique index over them, and
/// changes no row, in a real PostgreSQL catalog: what was typed stays, and two bodies may hold the same acronym.
/// </summary>
public sealed class HideIssuingAuthorityAcronymMigrationTests
{
    private const string PreviousMigration = "20260929142114_SeedMunicipalGuardCatalog";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_KeepsEveryStoredAcronym_AndLetsTwoBodiesShareOne()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(HideIssuingAuthorityAcronymMigrationTests));
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
        var before = await AcronymsAsync(connection);
        var inss = Guid.CreateVersion7();
        await InsertAsync(connection, inss, "Instituto Nacional do Seguro Social", "INSS");

        await migrator.MigrateAsync();

        (await AcronymsAsync(connection)).Should().Contain(before).And.Contain(("Instituto Nacional do Seguro Social", "INSS", "INSS"));
        var second = async () => await InsertAsync(connection, Guid.CreateVersion7(), "Outro Instituto", "INSS");
        await second.Should().NotThrowAsync("the unique index over the acronym is gone");
        var none = async () => await ExecuteAsync(
            connection,
            $"INSERT INTO catalog.issuing_authorities (id, name, normalized_name, created_at, is_deleted) VALUES ('{Guid.CreateVersion7()}', 'Sem sigla', 'SEM SIGLA', now(), false)");
        await none.Should().NotThrowAsync("a new body stores no acronym");
    }

    private static async Task InsertAsync(NpgsqlConnection connection, Guid id, string name, string acronym) =>
        await ExecuteAsync(
            connection,
            $"""
            INSERT INTO catalog.issuing_authorities (id, name, acronym, normalized_name, normalized_acronym, created_at, is_deleted)
            VALUES ('{id}', '{name}', '{acronym}', upper('{name}'), '{acronym}', now(), false)
            """);

    private static async Task<List<(string Name, string? Acronym, string? NormalizedAcronym)>> AcronymsAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            "SELECT name, acronym, normalized_acronym FROM catalog.issuing_authorities ORDER BY name",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<(string, string?, string?)>();
        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return rows;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
