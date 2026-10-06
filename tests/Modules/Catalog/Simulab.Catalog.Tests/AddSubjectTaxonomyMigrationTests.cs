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
/// F-79 AC1 (BR1, BR2, BR6): applied to a database that has the previous migration, the new one seeds the nine
/// areas in the order of the approved list and creates the subjects and topics tables, in a real PostgreSQL.
/// </summary>
public sealed class AddSubjectTaxonomyMigrationTests
{
    private const string PreviousMigration = "20261002114230_NormalizeExamStates";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_SeedsTheNineAreasInOrder_AndCreatesTheSubjectsAndTopicsTables()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddSubjectTaxonomyMigrationTests));
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
        (await TablesAsync(connection)).Should().NotContain(["areas", "subjects", "topics"]);

        await migrator.MigrateAsync();

        (await TablesAsync(connection)).Should().Contain(["areas", "subjects", "topics"]);
        (await AreasAsync(connection)).Should().Equal(
            ("Languages", 1),
            ("Mathematics", 2),
            ("LogicalReasoning", 3),
            ("NaturalSciences", 4),
            ("HumanSciences", 5),
            ("Law", 6),
            ("InformationTechnology", 7),
            ("Administration", 8),
            ("SpecificKnowledge", 9));
    }

    private static async Task<List<string>> TablesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<List<(string Code, int Order)>> AreasAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT code, display_order FROM {Schema}.areas ORDER BY display_order", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var areas = new List<(string, int)>();
        while (await reader.ReadAsync())
        {
            areas.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        return areas;
    }
}
