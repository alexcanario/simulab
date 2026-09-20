using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Simulab.Email;
using Simulab.Jobs.Persistence;
using Simulab.Persistence;
using Simulab.Testing;

namespace Simulab.Jobs.Tests;

/// <summary>
/// The queue on a real PostgreSQL database of its own, wired the way the Api host wires it
/// (<see cref="JobsServiceCollectionExtensions.AddJobs"/>), with a clock the test moves and a handler
/// the test controls. The hosted worker is never started: the test drives <see cref="JobRunner"/>.
/// </summary>
public sealed class JobTestHost : IAsyncDisposable
{
    private ServiceProvider _services = null!;
    private string _connectionString = string.Empty;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

    public RecordingJobHandler Handler { get; } = new();

    public RecordingEmailSender Emails { get; } = new();

    /// <summary>
    /// The runner, as a worker holds it. Calling it from several tasks at once is what a second Api
    /// instance does: every attempt opens its own scope and its own database connection (BR8).
    /// </summary>
    public JobRunner Runner => _services.GetRequiredService<JobRunner>();

    public static async Task<JobTestHost> StartAsync(string name)
    {
        var host = new JobTestHost();
        await host.InitializeAsync(name);
        return host;
    }

    /// <summary>Enqueues one job and returns its id. Test payloads are unique, which is how it is found again.</summary>
    public async Task<Guid> EnqueueAsync(string type, string payload)
    {
        await using var scope = _services.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
        queue.Enqueue(type, payload);
        await queue.FlushAsync();

        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        return await context.Jobs.Where(job => job.Payload == payload).Select(job => job.Id).SingleAsync();
    }

    /// <summary>Leaves a job exactly as a worker killed mid-send would leave it (BR10).</summary>
    public async Task SetRunningAsync(Guid id, DateTimeOffset startedAt)
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        await context.Jobs
            .Where(job => job.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, JobStatus.Running)
                .SetProperty(job => job.StartedAt, startedAt));
    }

    public async Task<Job?> FindAsync(Guid id)
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        return await context.Jobs.AsNoTracking().SingleOrDefaultAsync(job => job.Id == id);
    }

    public async Task<int> CountAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        return await context.Jobs.CountAsync();
    }

    private async Task InitializeAsync(string name)
    {
        _connectionString = await PostgresServer.CreateDatabaseAsync(name);
        _services = BuildServices(_connectionString);
        await _services.MigrateJobsAsync();
    }

    private ServiceProvider BuildServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddModulePersistence();
        services.AddSingleton<IEmailSender>(Emails);
        services.AddJobs(new ConfigurationBuilder().Build(), connectionString);
        services.AddJobQueueFor<JobsDbContext>();
        services.AddScoped<IJobHandler>(_ => Handler);
        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();

        // EF pools connections per connection string for the whole process (profile: test strategy).
        using var connection = new NpgsqlConnection(_connectionString);
        NpgsqlConnection.ClearPool(connection);
    }
}
