using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Simulab.Jobs.Tests;

/// <summary>
/// F-27 AC7 and AC8: what the worker's loop does around the cleanup. The runner and the queue are the real
/// ones, on a real database; only the cleanup's scope factory is replaced, which is the one way to make it
/// throw without inventing a broken database.
/// </summary>
public sealed class JobWorkerTests : IAsyncLifetime
{
    private JobTestHost _host = null!;

    public async Task InitializeAsync() => _host = await JobTestHost.StartAsync(nameof(JobWorkerTests));

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private JobWorker Worker(JobCleanup cleanup, bool enabled = true) =>
        new(_host.Runner,
            cleanup,
            Options.Create(new JobOptions { WorkerEnabled = enabled }),
            _host.Clock,
            _host.Logs.CreateLogger<JobWorker>());

    // AC7: a cleanup that throws is logged, and the jobs of that poll still run.
    [Fact]
    public async Task Poll_TheCleanupThrows_ItIsLoggedAndTheJobsStillRun()
    {
        await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        var worker = Worker(new JobCleanup(new ThrowingScopeFactory(), _host.Clock, _host.Logs.CreateLogger<JobCleanup>()));

        using var stopping = new CancellationTokenSource();
        var running = worker.StartAsync(stopping.Token);
        await WaitUntil(() => _host.Handler.Handled.Count == 1);
        await stopping.CancelAsync();
        await worker.StopAsync(CancellationToken.None);
        await running;

        // Equal(params string[]) would read the reason as a second expected item, so the reason goes here.
        _host.Handler.Handled.Should().ContainSingle("a broken cleanup must not stop the queue").Which.Should().Be("work");
        _host.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error && entry.Message.Contains("cleanup", StringComparison.OrdinalIgnoreCase));
    }

    // AC8: switched off means both, not just the jobs.
    [Fact]
    public async Task Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup()
    {
        await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        var expired = await _host.SeedAsync(JobStatus.Failed, _host.Clock.GetUtcNow() - TimeSpan.FromDays(91));
        var worker = Worker(_host.Cleanup, enabled: false);

        await worker.StartAsync(CancellationToken.None);

        // B-19: StartAsync only queues the body — `Task.Run(() => ExecuteAsync(stoppingCts.Token), stoppingCts.Token)`
        // — and StopAsync cancels that very token. A work item the thread pool has not dequeued yet is then
        // dropped without ever running, which under the load of the whole solution made this test pass its first
        // two assertions for the wrong reason: nothing had run at all. Wait for the worker to reach its decision,
        // so what follows is about a worker that ran and chose to do nothing.
        await WaitUntil(() => _host.Logs.Entries.Any(entry =>
            entry.Message.Contains("switched off", StringComparison.OrdinalIgnoreCase)));

        _host.Clock.Advance(JobPolicy.CleanupInterval);
        await worker.StopAsync(CancellationToken.None);

        _host.Handler.Handled.Should().BeEmpty();
        (await _host.FindAsync(expired)).Should().NotBeNull("the cleanup is part of the worker, not beside it");
        _host.Logs.Entries.Should().Contain(entry => entry.Message.Contains("switched off", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Polls the condition instead of sleeping a fixed time, so the test is not a race.</summary>
    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(50);
        }

        condition().Should().BeTrue("the worker should have got there within five seconds");
    }

    /// <summary>The one way to make the cleanup throw for real: it cannot open a scope.</summary>
    private sealed class ThrowingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new InvalidOperationException("the scope factory is broken on purpose");
    }
}
