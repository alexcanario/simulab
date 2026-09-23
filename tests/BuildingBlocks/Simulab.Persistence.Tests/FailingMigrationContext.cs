using Microsoft.EntityFrameworkCore;

namespace Simulab.Persistence.Tests;

/// <summary>A context whose only migration fails, to prove a real failure still reaches the log (F-19).</summary>
public sealed class FailingMigrationContext(DbContextOptions<FailingMigrationContext> options) : DbContext(options)
{
    public const string SchemaName = "failing";
}
