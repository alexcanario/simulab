using Microsoft.EntityFrameworkCore;
using Simulab.SharedKernel.Security;

namespace Simulab.Persistence.Tests;

/// <summary>A module context whose only migration fails, to prove a real failure still reaches the log (F-19).</summary>
public sealed class FailingMigrationContext(DbContextOptions<FailingMigrationContext> options, ICurrentTenant currentTenant)
    : ModuleDbContext(options, currentTenant)
{
    public const string SchemaName = "failing";

    protected override string Schema => SchemaName;
}
