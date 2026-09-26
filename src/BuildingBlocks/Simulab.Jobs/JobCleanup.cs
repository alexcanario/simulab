using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Jobs.Persistence;

namespace Simulab.Jobs;

/// <summary>
/// F-27: forgets the failed jobs that are older than <see cref="JobPolicy.FailedRetention"/>. The worker calls
/// <see cref="RunIfDueAsync"/> on every poll and this class decides whether an hour has passed, so no test ever
/// waits for a timer — the same shape that keeps <see cref="JobRunner"/> testable (F-13 BR13).
/// </summary>
public sealed partial class JobCleanup(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<JobCleanup> logger)
{
    // Computed once: CA1873 refuses an argument the logger may never use, and TotalDays is a division.
    private static readonly int RetentionDays = (int)JobPolicy.FailedRetention.TotalDays;

    private DateTimeOffset? _lastRun;

    /// <summary>
    /// Runs the cleanup when it is due and returns how many rows it removed, or null when it was not due.
    /// The first call after the process starts is always due (BR6): a host that never lives a whole hour
    /// would otherwise never clean.
    /// </summary>
    public async Task<int?> RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (_lastRun is { } last && now - last < JobPolicy.CleanupInterval)
        {
            return null;
        }

        _lastRun = now;
        return await RunAsync(now, cancellationToken);
    }

    /// <summary>
    /// Deletes the failed rows older than the retention, whether or not one is due. Age is counted from
    /// <see cref="Job.CreatedAt"/> (BR2), and only <see cref="JobStatus.Failed"/> rows are touched: an old
    /// <see cref="JobStatus.Running"/> row is a job a worker still takes again (F-13 BR10), not rubbish.
    /// </summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default) =>
        await RunAsync(timeProvider.GetUtcNow(), cancellationToken);

    private async Task<int> RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expiredBefore = now - JobPolicy.FailedRetention;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        var removed = await context.Jobs
            .Where(job => job.Status == JobStatus.Failed && job.CreatedAt < expiredBefore)
            .ExecuteDeleteAsync(cancellationToken);

        // BR7: silence when there was nothing to remove, or the log grows by a line an hour forever.
        if (removed > 0)
        {
            LogRemoved(logger, removed, RetentionDays);
        }

        return removed;
    }

    // Source-generated: CA1873 refuses arguments the logger may never need, and two ints would box on every call.
    [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Removed} failed job(s) older than {Days} days.")]
    private static partial void LogRemoved(ILogger logger, int removed, int days);
}
