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
/// F-77 AC1: applied to a database that has the previous migration, the new one creates the two alias tables
/// with restricted foreign keys and unique indexes that are NULLS NOT DISTINCT over live rows only, in a real
/// PostgreSQL, and going back drops them and leaves the subjects and the topics alone.
/// </summary>
public sealed class AddSubjectTopicAliasesMigrationTests
{
    private const string PreviousMigration = "20261008193457_AddNoticeSubjectMappings";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_CreatesTheTablesTheirKeysAndTheirIndexes_AndDownDropsThem()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(AddSubjectTopicAliasesMigrationTests));
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
        var before = await ColumnAsync(connection, TablesSql);
        before.Should().NotContain("subject_aliases").And.NotContain("topic_aliases");

        await migrator.MigrateAsync();

        var after = await ColumnAsync(connection, TablesSql);
        after.Should().Contain("subject_aliases").And.Contain("topic_aliases");
        (await ColumnAsync(connection, IndexesSql("subject_aliases"))).Should().BeEquivalentTo(
            "pk_subject_aliases",
            "ix_subject_aliases_subject",
            "ux_subject_aliases_tenant_normalized_name");
        (await ColumnAsync(connection, IndexesSql("topic_aliases"))).Should().BeEquivalentTo(
            "pk_topic_aliases",
            "ix_topic_aliases_topic",
            "ix_topic_aliases_normalized_name",
            "ux_topic_aliases_tenant_topic_normalized_name");

        foreach (var (constraint, parent) in new[]
                 {
                     ("fk_subject_aliases_subject", "subjects"),
                     ("fk_topic_aliases_topic", "topics")
                 })
        {
            (await ColumnAsync(connection, $"SELECT delete_rule FROM information_schema.referential_constraints WHERE constraint_name = '{constraint}'"))
                .Should().ContainSingle().Which.Should().Be("RESTRICT");
            (await ColumnAsync(connection, $"SELECT table_name FROM information_schema.constraint_column_usage WHERE constraint_name = '{constraint}'"))
                .Should().AllBe(parent);
        }

        foreach (var index in new[] { "ux_subject_aliases_tenant_normalized_name", "ux_topic_aliases_tenant_topic_normalized_name" })
        {
            var definition = (await ColumnAsync(connection, $"SELECT indexdef FROM pg_indexes WHERE schemaname = '{Schema}' AND indexname = '{index}'"))
                .Should().ContainSingle().Subject;
            definition.Should().Contain("UNIQUE", Exactly.Once());
            definition.Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
            definition.Should().Contain("is_deleted = false", Exactly.Once());
        }

        await migrator.MigrateAsync(PreviousMigration);

        var reverted = await ColumnAsync(connection, TablesSql);
        reverted.Should().NotContain("subject_aliases").And.NotContain("topic_aliases");
        reverted.Should().Contain("subjects").And.Contain("topics");
    }

    private static string TablesSql =>
        $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}'";

    private static string IndexesSql(string table) =>
        $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = '{table}'";

    private static async Task<List<string>> ColumnAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
