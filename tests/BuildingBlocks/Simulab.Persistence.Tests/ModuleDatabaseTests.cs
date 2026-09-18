using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Persistence.Tests;

/// <summary>
/// Base for the persistence tests: one database per test class on the shared container,
/// and a fresh context per call with the test's user, tenant and clock.
/// </summary>
public abstract class ModuleDatabaseTests : IAsyncLifetime
{
    private string _connectionString = string.Empty;

    protected TestUser User { get; } = new();

    protected TestTenant Tenant { get; } = new();

    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        _connectionString = await PostgresServer.CreateDatabaseAsync(GetType().Name);
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected SampleContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICurrentUser>(User);
        services.AddSingleton<ICurrentTenant>(Tenant);
        services.AddModulePersistence();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<SampleContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseModuleHistoryTable(SampleContext.SchemaName))
            .UseModuleConventions(provider)
            .Options;

        return new SampleContext(options, Tenant);
    }

    protected string ConnectionString => _connectionString;
}
