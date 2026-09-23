using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Jobs.Persistence;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-19: the Api applies the Jobs and Identity migrations on start (Program.cs). On an empty database,
/// the first start used to log one false <c>fail ... Database.Command[20102]</c> per context.
/// </summary>
public sealed class FirstStartLogTests
{
    [Fact]
    public async Task Start_EmptyDatabase_LogsNoErrorAndAppliesEveryMigration()
    {
        var logs = new RecordingLoggerProvider();
        await using var factory = CreateFactory(logs);
        await factory.PrepareAsync(nameof(FirstStartLogTests));

        _ = factory.Services;

        Errors(logs).Should().BeEmpty();
        await AssertEveryMigrationAppliedAsync<JobsDbContext>(factory);
        await AssertEveryMigrationAppliedAsync<IdentityModuleDbContext>(factory);
        await AssertEveryMigrationAppliedAsync<CatalogModuleDbContext>(factory);
    }

    [Fact]
    public async Task Start_MigratedDatabase_AppliesNothingAndLogsNoError()
    {
        string connectionString;
        await using (var first = CreateFactory(new RecordingLoggerProvider()))
        {
            await first.PrepareAsync(nameof(Start_MigratedDatabase_AppliesNothingAndLogsNoError));
            _ = first.Services;
            connectionString = first.ConnectionString;
        }

        var logs = new RecordingLoggerProvider();
        await using var second = CreateFactory(logs);
        await second.PrepareExistingAsync(connectionString);

        _ = second.Services;

        Errors(logs).Should().BeEmpty();
        logs.Entries.Should().NotContain(entry => entry.EventId.Id == RelationalEventId.MigrationApplying.Id);
        // F-33 added the catalog context to the three the host migrates on start.
        logs.Entries.Count(entry => entry.EventId.Id == RelationalEventId.MigrationsNotApplied.Id)
            .Should().Be(3, "the jobs, identity and catalog contexts each report the database is already up to date");
    }

    private static IdentityApiFactory CreateFactory(RecordingLoggerProvider logs) =>
        new() { ConfigureHost = builder => builder.ConfigureLogging(logging => logging.AddProvider(logs)) };

    private static List<RecordedLogEntry> Errors(RecordingLoggerProvider logs) =>
        [.. logs.Entries.Where(entry => entry.Level >= LogLevel.Error)];

    private static async Task AssertEveryMigrationAppliedAsync<TContext>(IdentityApiFactory factory)
        where TContext : DbContext
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<TContext>().Database;

        (await database.GetAppliedMigrationsAsync()).Should().Equal(database.GetMigrations());
    }
}
