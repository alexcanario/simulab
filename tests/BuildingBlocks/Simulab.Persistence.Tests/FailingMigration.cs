using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Simulab.Persistence.Tests;

/// <summary>Reads a table that does not exist, as a broken migration would.</summary>
[DbContext(typeof(FailingMigrationContext))]
[Migration("20260923000000_Failing")]
public sealed class FailingMigration : Migration
{
    public const string MissingTable = "failing.missing_table";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql($"SELECT * FROM {MissingTable}");
    }
}
