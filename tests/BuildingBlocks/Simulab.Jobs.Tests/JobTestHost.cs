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

    /// <summary>F-27 AC6: what the code logged, so a test can read it instead of assuming it.</summary>
    public RecordingLoggerProvider Logs { get; } = new();

    /// <summary>
    /// The runner, as a worker holds it. Calling it from several tasks at once is what a second Api
    /// instance does: every attempt opens its own scope and its own database connection (BR8).
    /// </summary>
    public JobRunner Runner => _services.GetRequiredService<JobRunner>();

    /// <summary>F-27: the cleanup, as the worker holds it — one instance, so it remembers its last run.</summary>
    public JobCleanup Cleanup => _services.GetRequiredService<JobCleanup>();

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

    /// <summary>Removes the row behind the runner's back, as a second worker finishing it first would (BR10).</summary>
    public async Task DeleteAsync(Guid id)
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        await context.Jobs.Where(job => job.Id == id).ExecuteDeleteAsync();
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

    /// <summary>
    /// F-27: one row in the state and at the age the test needs. `created_at` is what the retention counts
    /// from (BR2), so the test writes it directly instead of moving the clock by three months.
    /// </summary>
    public async Task<Guid> SeedAsync(JobStatus status, DateTimeOffset createdAt, string payload = "")
    {
        var id = Guid.CreateVersion7();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var insert = new NpgsqlCommand(
            $"""
            INSERT INTO {JobsDbContext.QualifiedTableName} (id, type, payload, status, attempts, created_at, run_after, last_error)
            VALUES ($1, 'test.seeded', $2, $3, 5, $4, $4, 'System.Exception')
            """,
            connection);
        insert.Parameters.Add(new NpgsqlParameter { Value = id });
        insert.Parameters.Add(new NpgsqlParameter { Value = payload });
        insert.Parameters.Add(new NpgsqlParameter { Value = (int)status });
        insert.Parameters.Add(new NpgsqlParameter { Value = createdAt });
        await insert.ExecuteNonQueryAsync();
        return id;
    }

    /// <summary>
    /// F-18: fills the table the way months of BR7 would, with rows given up on that no claim may read, and
    /// refreshes the statistics the planner chooses by.
    /// </summary>
    public async Task SeedFailedAsync(int count)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var insert = new NpgsqlCommand(
            $"""
            INSERT INTO {JobsDbContext.QualifiedTableName} (id, type, payload, status, attempts, created_at, run_after, last_error)
            SELECT gen_random_uuid(), 'test.failed', '', {(int)JobStatus.Failed}, 5, $1 - n * interval '1 minute', $1, 'System.Exception'
            FROM generate_series(1, $2) AS n
            """,
            connection);
        insert.Parameters.Add(new NpgsqlParameter { Value = Clock.GetUtcNow() });
        insert.Parameters.Add(new NpgsqlParameter { Value = count });
        await insert.ExecuteNonQueryAsync();

        await using var analyze = new NpgsqlCommand($"ANALYZE {JobsDbContext.QualifiedTableName}", connection);
        await analyze.ExecuteNonQueryAsync();
    }

    /// <summary>F-18: the plan PostgreSQL picks for the runner's own claim statement, as text.</summary>
    public async Task<string> ExplainClaimAsync()
    {
        var now = Clock.GetUtcNow();
        var sql = JobRunner.ClaimSql.Replace("{0}", "$1", StringComparison.Ordinal).Replace("{1}", "$2", StringComparison.Ordinal);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var explain = new NpgsqlCommand($"EXPLAIN {sql}", connection);
        explain.Parameters.Add(new NpgsqlParameter { Value = now });
        explain.Parameters.Add(new NpgsqlParameter { Value = now - JobPolicy.StaleAfter });

        var lines = new List<string>();
        await using var reader = await explain.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>F-18: the definitions of the job table's indexes, as PostgreSQL states them.</summary>
    public async Task<IReadOnlyList<string>> IndexDefinitionsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var query = new NpgsqlCommand(
            $"SELECT indexdef FROM pg_indexes WHERE schemaname = '{JobsDbContext.SchemaName}' AND tablename = '{JobsDbContext.TableName}' ORDER BY indexname",
            connection);

        var definitions = new List<string>();
        await using var reader = await query.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            definitions.Add(reader.GetString(0));
        }

        return definitions;
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
        // F-27: Information, because the cleanup's one line is an acceptance criterion.
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddProvider(Logs));
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
