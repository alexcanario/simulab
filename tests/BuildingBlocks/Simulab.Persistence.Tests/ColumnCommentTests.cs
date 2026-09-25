using Npgsql;
using Simulab.Persistence.Conventions;

namespace Simulab.Persistence.Tests;

/// <summary>
/// F-24 AC2 and AC4: the descriptions of the repeated columns reach a real PostgreSQL catalog through
/// the migration, on an entity whose configuration writes none of them by hand.
/// </summary>
public class ColumnCommentTests : ModuleDatabaseTests
{
    private async Task<string?> ColumnCommentAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT col_description(c.oid, a.attnum)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid
            WHERE n.nspname = @schema AND c.relname = @table AND a.attname = @column
            """;
        command.Parameters.AddWithValue("schema", SampleContext.SchemaName);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);

        return await command.ExecuteScalarAsync(CancellationToken.None) as string;
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("created_at")]
    [InlineData("created_by")]
    [InlineData("updated_at")]
    [InlineData("updated_by")]
    [InlineData("is_deleted")]
    [InlineData("deleted_at")]
    [InlineData("deleted_by")]
    [InlineData("id")]
    public async Task TheRepeatedColumns_CarryTheirDescriptionInTheDatabase(string column)
    {
        // SampleItem is a TenantEntity and its configuration describes nothing: whatever is here came
        // from the convention (AC2), through the migration, into the catalog (AC4).
        var comment = await ColumnCommentAsync("items", column);

        comment.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TheDescriptionInTheDatabase_IsTheOneWeWrote()
    {
        (await ColumnCommentAsync("items", "is_deleted")).Should().Be(StandardColumnComments.IsDeleted);
        (await ColumnCommentAsync("items", "tenant_id")).Should().Be(StandardColumnComments.TenantId);
    }
}
