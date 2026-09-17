using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Simulab.Persistence.Tests;

/// <summary>AC1: a module context puts its tables and its migrations history in its own schema, in snake_case.</summary>
public class SchemaTests : ModuleDatabaseTests
{
    private async Task<List<string>> TablesAsync(string schema)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema ORDER BY table_name";
        command.Parameters.AddWithValue("schema", schema);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    [Fact]
    public async Task Tables_AreCreatedInTheModuleSchema_WithSnakeCaseNames()
    {
        var tables = await TablesAsync(SampleContext.SchemaName);

        tables.Should().Contain(["items", "notes"]);
        (await TablesAsync("public")).Should().BeEmpty("a module keeps nothing in the public schema");
    }

    [Fact]
    public async Task Columns_UseSnakeCaseNames()
    {
        await using var context = CreateContext();

        var columns = context.Model.FindEntityType(typeof(SampleItem))!
            .GetProperties()
            .Select(property => property.GetColumnName())
            .ToList();

        columns.Should().Contain(["created_at", "created_by", "is_deleted", "tenant_id", "name"]);
    }

    [Fact]
    public async Task MigrationsHistoryTable_IsCreatedInTheModuleSchema()
    {
        await using var context = CreateContext();

        await context.GetService<IHistoryRepository>().CreateIfNotExistsAsync(CancellationToken.None);

        (await TablesAsync(SampleContext.SchemaName))
            .Should().Contain(PersistenceServiceCollectionExtensions.HistoryTableName);
    }
}
