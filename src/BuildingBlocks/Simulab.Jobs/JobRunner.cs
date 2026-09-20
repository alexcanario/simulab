using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Jobs.Persistence;

namespace Simulab.Jobs;

/// <summary>
/// Takes one job at a time and runs it (F-13 BR5-BR10). The worker calls it in a loop; a test calls
/// <see cref="RunPendingAsync"/> directly, which is what keeps the tests free of waiting (BR13).
/// </summary>
public sealed class JobRunner(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<JobRunner> logger)
{
    /// <summary>
    /// BR8, BR9, BR10: one statement claims the oldest due row and marks it <see cref="JobStatus.Running"/>.
    /// <c>SKIP LOCKED</c> is what lets a second Api instance poll the same table without ever taking the
    /// row this one is taking. A row left <c>Running</c> by a stopped worker becomes due again.
    /// </summary>
    private const string ClaimSql = $$"""
        UPDATE {{JobsDbContext.QualifiedTableName}} SET status = {1}, started_at = {0}, attempts = attempts + 1
        WHERE id = (
            SELECT id FROM {{JobsDbContext.QualifiedTableName}}
            WHERE (status = {2} AND run_after <= {0}) OR (status = {1} AND started_at <= {3})
            ORDER BY created_at
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        RETURNING *
        """;

    /// <summary>Runs every job that is due, oldest first, until none is left. Returns how many ran.</summary>
    public async Task<int> RunPendingAsync(CancellationToken cancellationToken = default)
    {
        var ran = 0;
        while (!cancellationToken.IsCancellationRequested && await RunNextAsync(cancellationToken))
        {
            ran++;
        }

        return ran;
    }

    /// <summary>Runs the next due job. False when there was none.</summary>
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

        var job = await ClaimAsync(context, cancellationToken);
        if (job is null)
        {
            return false;
        }

        try
        {
            var handler = scope.ServiceProvider.GetServices<IJobHandler>()
                .FirstOrDefault(candidate => candidate.Type == job.Type)
                ?? throw new InvalidOperationException($"No handler is registered for job type '{job.Type}'.");

            await handler.HandleAsync(job.Payload, cancellationToken);

            // BR6: the job is gone, with the live link and the address it carried.
            context.Jobs.Remove(job);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await FailAsync(context, job, exception, cancellationToken);
        }

        return true;
    }

    private async Task<Job?> ClaimAsync(JobsDbContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var stale = now - JobPolicy.StaleAfter;

        var claimed = await context.Jobs
            .FromSqlRaw(ClaimSql, now, (int)JobStatus.Running, (int)JobStatus.Pending, stale)
            .ToListAsync(cancellationToken);

        return claimed.Count == 0 ? null : claimed[0];
    }

    /// <summary>BR5, BR7: back off and try again, or give up and keep the row as the evidence.</summary>
    private async Task FailAsync(JobsDbContext context, Job job, Exception exception, CancellationToken cancellationToken)
    {
        job.LastError = Truncate(exception.Message);

        if (job.Attempts >= JobPolicy.MaxAttempts)
        {
            job.Status = JobStatus.Failed;
            logger.LogError(
                exception,
                "Job {JobId} of type {JobType} failed {Attempts} times and was given up on.",
                job.Id, job.Type, job.Attempts);
        }
        else
        {
            job.Status = JobStatus.Pending;
            job.RunAfter = timeProvider.GetUtcNow().Add(JobPolicy.BackoffAfter(job.Attempts));
            logger.LogWarning(
                exception,
                "Job {JobId} of type {JobType} failed on attempt {Attempts}; it runs again after {RunAfter}.",
                job.Id, job.Type, job.Attempts, job.RunAfter);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string message) =>
        message.Length <= Persistence.Configurations.JobConfiguration.LastErrorMaxLength
            ? message
            : message[..Persistence.Configurations.JobConfiguration.LastErrorMaxLength];
}
