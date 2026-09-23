using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Simulab.Testing;

namespace Simulab.Persistence.Tests;

/// <summary>F-19 AC3: creating the history table first quiets only the probe; a real failure is still logged and thrown.</summary>
public sealed class ModuleMigrationTests
{
    [Fact]
    public async Task MigrateModuleAsync_FailingMigration_LogsTheFailureAndThrows()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(ModuleMigrationTests));
        var logs = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        services.AddDbContext<FailingMigrationContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.UseModuleHistoryTable(FailingMigrationContext.SchemaName)));
        await using var provider = services.BuildServiceProvider();

        var migrate = () => provider.MigrateModuleAsync<FailingMigrationContext>();

        await migrate.Should().ThrowAsync<PostgresException>();
        logs.Entries.Where(entry => entry.Level >= LogLevel.Error && entry.EventId.Id == RelationalEventId.CommandError.Id)
            .Should().ContainSingle()
            .Which.Message.Should().Contain(FailingMigration.MissingTable, "only the broken migration fails, not the history probe");

        using var connection = new NpgsqlConnection(connectionString);
        NpgsqlConnection.ClearPool(connection);
    }
}
